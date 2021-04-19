using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Xml;
using System.Xml.Serialization;

public class BackupAndRestoreData
{

	public static Data GetSettings(string file)
	{
		return Deserialize<Data>(file);
	}

	public static void SetSettings(string file, IEnumerable<Connection> connections, IEnumerable<Item> items)
	{
		var data = new Data();
		foreach (var item in connections)
			data.Connections.Add(item);
		foreach (var item in items)
			data.Items.Add(item);
		Serialize(data, file);
	}

    public static T Deserialize<T>(string path)
	{
		if (!File.Exists(path))
			return default(T);
		var xml = File.ReadAllText(path);
		var sr = new StringReader(xml);
		var settings = new XmlReaderSettings();
		settings.DtdProcessing = DtdProcessing.Ignore;
		settings.XmlResolver = null;
		var reader = XmlReader.Create(sr, settings);
		var serializer = new XmlSerializer(typeof(T), new Type[] { typeof(T) });
		var o = (T)serializer.Deserialize(reader);
		reader.Dispose();
		return o;
	}

	public static void Serialize<T>(T o, string path)
	{
		var settings = new XmlWriterSettings();
		//settings.OmitXmlDeclaration = true;
		settings.Encoding = System.Text.Encoding.UTF8;
		settings.Indent = true;
		settings.IndentChars = "\t";
		var serializer = new XmlSerializer(typeof(T));
		// Serialize in memory first, so file will be locked for shorter times.
		var ms = new MemoryStream();
		var xw = XmlWriter.Create(ms, settings);
		serializer.Serialize(xw, o);
		File.WriteAllBytes(path, ms.ToArray());
	}
	
	/// <summary>
	/// Reformat XML document.
	/// </summary>
	/// <param name="xml"></param>
	/// <returns></returns>
	public static string XmlFormat(string xml)
	{
		var xd = new XmlDocument();
		xd.XmlResolver = null;
		xd.LoadXml(xml);
		var sb = new StringBuilder();
		var xws = new XmlWriterSettings();
		xws.Indent = true;
		xws.CheckCharacters = true;
		var xw = XmlTextWriter.Create(sb, xws);
		xd.WriteTo(xw);
		xw.Close();
		return sb.ToString();
	}

	public static List<Item> GetSchemas(string connectionString)
	{
		// Get available schemas.
		var cmd = new SqlCommand("SELECT name FROM sys.schemas");
		cmd.CommandType = CommandType.Text;
		var table = ExecuteDataTable(connectionString, cmd);
		var schemaNames = table.Rows.Cast<DataRow>()
			.Select(x => (string)x["name"])
			.OrderBy(x => x)
			.ToList();
		var items = new List<Item>();
		foreach (var schemaName in schemaNames)
		{
			var item = new Item() { Schema = schemaName };
			items.Add(item);
		}
		return items;
	}

	public static List<Item> GetColumns(string connectionString, string schemaName, string tableName)
	{
		var sql = "";
		sql += "SELECT c.[name], [type] = ut.[name]\r\n";
		sql += "FROM sys.columns c\r\n";
		sql += "INNER JOIN sys.tables t ON t.[object_id] = c.[object_id]\r\n";
		sql += "INNER JOIN sys.systypes ut ON ut.xusertype = c.user_type_id\r\n";
		sql += "WHERE SCHEMA_NAME(t.[schema_id]) = @schema_name AND OBJECT_NAME(c.[object_id]) = @table_name\r\n";
		sql += "ORDER BY c.column_id";
		// Get available columns.
		var cmd = new SqlCommand(sql);
		cmd.CommandType = CommandType.Text;
		cmd.Parameters.AddWithValue("@schema_name", schemaName);
		cmd.Parameters.AddWithValue("@table_name", tableName);
		var table = ExecuteDataTable(connectionString, cmd);
		var items = new List<Item>();
		var rows = table.Rows.Cast<DataRow>();
		foreach (var row in rows)
		{
			var item = new Item()
			{
				Schema = schemaName,
				Table = tableName,
				Column = (string)row["name"],
				Type = (string)row["type"],
			};
			items.Add(item);
		}
		return items;
	}

	public static List<Item> GetTables(string connectionString, string schemaName)
	{
		// Get available schemas.
		var cmd = new SqlCommand("SELECT name FROM sys.tables WHERE SCHEMA_NAME(schema_id) = @schema_name");
		cmd.CommandType = CommandType.Text;
		cmd.Parameters.AddWithValue("@schema_name", schemaName);
		var table = ExecuteDataTable(connectionString, cmd);
		var tableNames = table.Rows.Cast<DataRow>()
			.Select(x => (string)x["name"])
			.OrderBy(x => x)
			.ToList();
		var items = new List<Item>();
		foreach (var tableName in tableNames)
		{
			var item = new Item() { Schema = schemaName, Table = tableName };
			items.Add(item);
		}
		return items;
	}

	public static T ExecuteDataSet<T>(string connectionString, SqlCommand cmd, string comment = null) where T : DataSet
	{
		var conn = new SqlConnection(connectionString);
		cmd.Connection = conn;
		conn.Open();
		var adapter = new SqlDataAdapter(cmd);
		var ds = Activator.CreateInstance<T>();
		int rowsAffected = ds.GetType() == typeof(DataSet)
			? adapter.Fill(ds)
			: adapter.Fill(ds, ds.Tables[0].TableName);
		adapter.Dispose();
		cmd.Dispose();
		// Dispose calls conn.Close() internally.
		conn.Dispose();
		return ds;
	}

	public static DataSet ExecuteDataSet(string connectionString, SqlCommand cmd, string comment = null)
	{
		return ExecuteDataSet<DataSet>(connectionString, cmd, comment);
	}

	public static DataTable ExecuteDataTable(string connectionString, SqlCommand cmd, string comment = null)
	{
		var ds = ExecuteDataSet(connectionString, cmd, comment);
		if (ds != null && ds.Tables.Count > 0)
			return ds.Tables[0];
		return null;
	}

	public static DataRow ExecuteDataRow(string connectionString, SqlCommand cmd, string comment = null)
	{
		var table = ExecuteDataTable(connectionString, cmd, comment);
		if (table != null && table.Rows.Count > 0)
			return table.Rows[0];
		return null;
	}

	public static string ScriptTable(string connectionString, string schemaName, string tableName)
	{
		var builder = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
		//System.Console.WriteLine("Host: {0}", builder.DataSource);
		//System.Console.WriteLine("Data: {0}", builder.InitialCatalog);
		//System.Console.WriteLine("ISec: {0}", builder.IntegratedSecurity);
		//System.Console.WriteLine("User: {0}", builder.UserID);
		//System.Console.WriteLine("Pass: {0}", builder.Password);
		var conn = new System.Data.SqlClient.SqlConnection(connectionString);
		var sc = new Microsoft.SqlServer.Management.Common.ServerConnection(conn);
		var server = new Microsoft.SqlServer.Management.Smo.Server(sc);
		//server.ConnectionContext.Connect();
		// Getting version automatically connects.
		//System.Console.WriteLine("Server Version: {0}",  server.Information.Version);
		var sb = new StringBuilder("");
		var database = server.Databases[builder.InitialCatalog];
		var table = database.Tables[tableName, schemaName];
		// Script DROP.
		var options = new Microsoft.SqlServer.Management.Smo.ScriptingOptions();
		// If IncludeIfNotExists = true then procedure text will be generated
		// through "EXEC dbo.sp_executesql @statement = N'".
		options.IncludeIfNotExists = true;
		options.ScriptDrops = true;
		var strings = table.Script(options);
		foreach (var s in strings)
			sb.AppendLine(s);
		sb.AppendLine();
		// Script CREATE.
		options = new Microsoft.SqlServer.Management.Smo.ScriptingOptions();
		//options.AppendToFile = true;
		options.ClusteredIndexes = true;
		options.NoCollation = true;
		//options.DriClustered  = false;
		//options.NonClusteredIndexes = true;
		//options.DriNonClustered = false;
		//options.Indexes = true;
		//options.DriIndexes = false;
		//options.FileName = fileInfo.FullName;
		//options.Permissions = true;
		strings = table.Script(options);
		foreach (var s in strings)
			sb.AppendLine(s);
		return sb.ToString();
	}

}

[XmlRoot("Data")]
public class Data
{

	public void Reset()
	{
		Options = new List<Option>();
		Options.Add(new Option(){ Name="UseVarChar", Value="False"});
		Options.Add(new Option(){ Name="Compress", Value="True"});
		Options.Add(new Option(){ Name="ANSI_PADDING", Value="OFF"});
		Connections = new List<Connection>();
		Items = new List<Item>();
	}

	[XmlArrayItem("Option")]
	public List<Option> Options { get; set; }

	[XmlArrayItem("Connection")]
	public List<Connection> Connections { get; set; }

	[XmlArrayItem("Item")]
	public List<Item> Items { get; set; }

}

public class Option
{
	[XmlAttribute] public string Name { get; set; }
	[XmlAttribute] public string Value { get; set; }
}

public class Connection
{
	[XmlAttribute] public string Name { get; set; }
	[XmlAttribute] public string Value { get; set; }
}

public class Item
{
	[XmlAttribute] public string Database { get; set; }
	[XmlAttribute] public string Schema { get; set; }
	[XmlAttribute] public string Table { get; set; }
	[XmlAttribute] public string Column { get; set; }
	[XmlAttribute] public string Query { get; set; }
	[XmlAttribute] public string Type { get; set; }
}
