//------------------------------------------------------------------------------
// <copyright file="SqlProfileProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Web.Profile;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Configuration.Provider;
using System.Configuration;
using System.Web.Hosting;
using System.Security;

namespace JocysCom.Web.Security
{
    /* TODO:
     * 
     * REVIEW:
     * - Strings that are too long will throw an exception saying data will be truncated...
     * - State where I couldn't log in??   ASPXANONYMOUS set
     * 
     */
    public class SqlStoredProcedureProfileProvider : ProfileProvider {
        private string _appName;
        private string _sqlConnectionString;
        private string _readSproc;
        private string _setSproc;
        private int _commandTimeout;

        public override void Initialize(string name, NameValueCollection config) {
            
            if (config == null)
                throw new ArgumentNullException("config");
            if (String.IsNullOrEmpty(name))
                name = "StoredProcedureDBProfileProvider";
            if (string.IsNullOrEmpty(config["description"])) {
                config.Remove("description");
                config.Add("description", "StoredProcedureDBProfileProvider");
            }
            base.Initialize(name, config);

            string temp = config["connectionStringName"];
            if (String.IsNullOrEmpty(temp))
                throw new ProviderException("connectionStringName not specified");
            _sqlConnectionString = GetConnectionString(temp);
            if (String.IsNullOrEmpty(_sqlConnectionString)) {
                throw new ProviderException("connectionStringName not specified");
            }

            _appName = config["applicationName"];
            if (string.IsNullOrEmpty(_appName))
                _appName = GetDefaultAppName();

            if (_appName.Length > 256) {
                throw new ProviderException("Application name too long");
            }

            _setSproc = config["setProcedure"];
            if (String.IsNullOrEmpty(_setSproc)) {
                throw new ProviderException("setProcedure not specified");
            }

            _readSproc = config["readProcedure"];
            if (String.IsNullOrEmpty(_readSproc)) {
                throw new ProviderException("readProcedure not specified");
            }

            string timeout = config["commandTimeout"];
            if (string.IsNullOrEmpty(timeout) || !Int32.TryParse(timeout, out _commandTimeout))
            {
                _commandTimeout = 30;
            }

            config.Remove("commandTimeout");
            config.Remove("connectionStringName");
            config.Remove("applicationName");
            config.Remove("readProcedure");
            config.Remove("setProcedure");
            if (config.Count > 0) {
                string attribUnrecognized = config.GetKey(0);
                if (!String.IsNullOrEmpty(attribUnrecognized))
                    throw new ProviderException("Unrecognized config attribute:" + attribUnrecognized);
            }
        }

        internal static string GetDefaultAppName() {
            try {
                string appName = HostingEnvironment.ApplicationVirtualPath;
                if (String.IsNullOrEmpty(appName)) {
                    appName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
                    int indexOfDot = appName.IndexOf('.');
                    if (indexOfDot != -1) {
                        appName = appName.Remove(indexOfDot);
                    }
                }

                if (String.IsNullOrEmpty(appName)) {
                    return "/";
                }
                else {
                    return appName;
                }
            }
            catch (SecurityException) {
                return "/";
            }
        }

        internal static string GetConnectionString(string specifiedConnectionString) {
            if (String.IsNullOrEmpty(specifiedConnectionString))
                return null;

            // Check <connectionStrings> config section for this connection string
            ConnectionStringSettings connObj = ConfigurationManager.ConnectionStrings[specifiedConnectionString];
            if (connObj != null)
                return connObj.ConnectionString;

            return null;
        }

        public override string ApplicationName {
            get { return _appName; }
            set {
                if (value == null) throw new ArgumentNullException("ApplicationName");
                if (value.Length > 256) {
                    throw new ProviderException("Application name too long");
                }
                _appName = value;

            }
        }

        private int CommandTimeout {
            get { return _commandTimeout; }
        }

        ////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        // customProviderData = "Varname;SqlDbType;size"
        private void GetProfileDataFromSproc(SettingsPropertyCollection properties, SettingsPropertyValueCollection svc, string username, SqlConnection conn, bool userIsAuthenticated)
        {

            SqlCommand cmd = CreateSprocSqlCommand(_readSproc, conn, username, userIsAuthenticated);
            try {
                cmd.Parameters.RemoveAt("@IsUserAnonymous"); //anonymous flag not needed on get

                List<ProfileColumnData> columnData = new List<ProfileColumnData>(properties.Count);
                foreach (SettingsProperty prop in properties) {
                    SettingsPropertyValue value = new SettingsPropertyValue(prop);
                    svc.Add(value);

                    string persistenceData = prop.Attributes["CustomProviderData"] as string;
                    // If we can't find the table/column info we will ignore this data
                    if (String.IsNullOrEmpty(persistenceData)) {
                        // REVIEW: Perhaps we should throw instead?
                        continue;
                    }
                    string[] chunk = persistenceData.Split(new char[] { ';' });
                    if (chunk.Length != 3) {
                        // REVIEW: Perhaps we should throw instead?
                        continue;
                    }
                    string varname = chunk[0];
                    // REVIEW: Should we ignore case?
                    SqlDbType datatype = (SqlDbType)Enum.Parse(typeof(SqlDbType), chunk[1], true);

                    int size = 0;
                    if (!Int32.TryParse(chunk[2], out size)) {
                        throw new ArgumentException("Unable to parse as integer: " + chunk[2]);
                    }

                    columnData.Add(new ProfileColumnData(varname, value, null /* not needed for get */, datatype));
                    cmd.Parameters.Add(CreateOutputParam(varname, datatype, size));
                }

                cmd.ExecuteNonQuery();
                for (int i = 0; i < columnData.Count; ++i) {
                    ProfileColumnData colData = columnData[i];
                    object val = cmd.Parameters[colData.VariableName].Value;
                    SettingsPropertyValue propValue = colData.PropertyValue;

                    //Only initialize a SettingsPropertyValue for non-null values
                    if (!(val is DBNull || val == null))
                    {
                        propValue.PropertyValue = val;
                        propValue.IsDirty = false;
                        propValue.Deserialized = true;
                    }
                }
            }
            finally {
                cmd.Dispose();
            }
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection) {
            SettingsPropertyValueCollection svc = new SettingsPropertyValueCollection();

            if (collection == null || collection.Count < 1 || context == null)
                return svc;

            string username = (string)context["UserName"];
            bool userIsAuthenticated = (bool)context["IsAuthenticated"];
            if (String.IsNullOrEmpty(username))
                return svc;

            SqlConnection conn = null;
            try {
                conn = new SqlConnection(_sqlConnectionString);
                conn.Open();

                GetProfileDataFromSproc(collection, svc, username, conn, userIsAuthenticated);
            }
            finally {
                if (conn != null) {
                    conn.Close();
                }
            }

            return svc;
        }

        ////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////

        // Container struct for use in aggregating columns for queries
        private struct ProfileColumnData {
            public string VariableName;
            public SettingsPropertyValue PropertyValue;
            public object Value;
            public SqlDbType DataType;

            public ProfileColumnData(string var, SettingsPropertyValue pv, object val, SqlDbType type) {
                VariableName = var;
                PropertyValue = pv;
                Value = val;
                DataType = type;
            }
        }

        // Helper that just sets up the usual sproc sqlcommand parameters and adds the applicationname/username
        private SqlCommand CreateSprocSqlCommand(string sproc, SqlConnection conn, string username, bool isAnonymous) {
            SqlCommand cmd = new SqlCommand(sproc, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = CommandTimeout;
            cmd.Parameters.AddWithValue("@ApplicationName", ApplicationName);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@IsUserAnonymous", isAnonymous);
            return cmd;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection) {
            string username = (string)context["UserName"];
            bool userIsAuthenticated = (bool)context["IsAuthenticated"];

            if (username == null || username.Length < 1 || collection.Count < 1)
                return;

            SqlConnection conn = null;
            SqlCommand cmd = null;
            try {
                bool anyItemsToSave = false;

                // First make sure we have at least one item to save
                foreach (SettingsPropertyValue pp in collection) {
                    if (pp.IsDirty) {
                        if (!userIsAuthenticated) {
                            bool allowAnonymous = (bool)pp.Property.Attributes["AllowAnonymous"];
                            if (!allowAnonymous)
                                continue;
                        }
                        anyItemsToSave = true;
                        break;
                    }
                }

                if (!anyItemsToSave)
                    return;

                conn = new SqlConnection(_sqlConnectionString);
                conn.Open();

                List<ProfileColumnData> columnData = new List<ProfileColumnData>(collection.Count);

                foreach (SettingsPropertyValue pp in collection) {
                    if (!userIsAuthenticated) {
                        bool allowAnonymous = (bool)pp.Property.Attributes["AllowAnonymous"];
                        if (!allowAnonymous)
                            continue;
                    }

                    //Unlike the table provider, the sproc provider works against a fixed stored procedure
                    //signature, and must provide values for each stored procedure parameter
                    //
                    //if (!pp.IsDirty && pp.UsingDefaultValue) // Not fetched from DB and not written to
                    //    continue;

                    string persistenceData = pp.Property.Attributes["CustomProviderData"] as string;
                    // If we can't find the table/column info we will ignore this data
                    if (String.IsNullOrEmpty(persistenceData)) {
                        // REVIEW: Perhaps we should throw instead?
                        continue;
                    }
                    string[] chunk = persistenceData.Split(new char[] { ';' });
                    if (chunk.Length != 3) {
                        // REVIEW: Perhaps we should throw instead?
                        continue;
                    }
                    string varname = chunk[0];
                    // REVIEW: Should we ignore case?
                    SqlDbType datatype = (SqlDbType)Enum.Parse(typeof(SqlDbType), chunk[1], true);
                    // chunk[2] = size, which we ignore

                    object value = null;

                    if (!pp.IsDirty && pp.UsingDefaultValue) // Not fetched from DB and not written to
                        value = DBNull.Value;
                    else if (pp.Deserialized && pp.PropertyValue == null) { // value was explicitly set to null
                        value = DBNull.Value;
                    }
                    else {
                        value = pp.PropertyValue;
                    }

                    // REVIEW: Might be able to ditch datatype
                    columnData.Add(new ProfileColumnData(varname, pp, value, datatype));
                }

                cmd = CreateSprocSqlCommand(_setSproc, conn, username, userIsAuthenticated);
                foreach (ProfileColumnData data in columnData) {
                    if (data.VariableName != "RegistrationDate")
                    {
                        cmd.Parameters.AddWithValue(data.VariableName, data.Value);
                        cmd.Parameters[data.VariableName].SqlDbType = data.DataType;
                    }
                }
                cmd.Parameters.AddWithValue("CurrentTimeUtc", DateTime.UtcNow);
                cmd.Parameters["CurrentTimeUtc"].SqlDbType = SqlDbType.DateTime;
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch(Exception)
                {
                    throw;
                }
            }
            finally {
                if (cmd != null)
                    cmd.Dispose();
                if (conn != null)
                    conn.Close();
            }
        }

        ////////////////////////////////////////////////////////////

        private static SqlParameter CreateOutputParam(string paramName, SqlDbType dbType, int size)
        {
            SqlParameter param = new SqlParameter(paramName, dbType);
            param.Direction = ParameterDirection.Output;
            param.Size = size;
            return param;
        }

        /////////////////////////////////////////////////////////////////////////////
        // Mangement APIs from ProfileProvider class

        public override int DeleteProfiles(ProfileInfoCollection profiles) {
            throw new NotSupportedException("This method is not supported for this provider.");
        }

        public override int DeleteProfiles(string[] usernames) {
            throw new NotSupportedException("This method is not supported for this provider.");
        }

        public override int DeleteInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate) {
            throw new NotSupportedException("This method is not supported for this provider.");
        }

        public override int GetNumberOfInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate) {
            throw new NotSupportedException("This method is not supported for this provider.");
        }

        public override ProfileInfoCollection GetAllProfiles(ProfileAuthenticationOption authenticationOption, int pageIndex, int pageSize, out int totalRecords) {
            throw new NotSupportedException("This method is not supported for this provider.");
        }

        public override ProfileInfoCollection GetAllInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords) {
            throw new NotSupportedException("This method is not supported for this provider.");
        }

        public override ProfileInfoCollection FindProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, int pageIndex, int pageSize, out int totalRecords) {
            throw new NotSupportedException("This method is not supported for this provider.");
        }

        public override ProfileInfoCollection FindInactiveProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords) {
            throw new NotSupportedException("This method is not supported for this provider.");
        }
    }
}





