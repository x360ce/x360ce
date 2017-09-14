using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Web
{
    public partial class DatabaseHelper
    {
        public static void Upsert(CloudMessage command, List<string> messages)
        {
            // Games can be inserted by using computer id only.
            messages.Add(Upsert(command.UserGames));
            messages.Add(Upsert(command.UserDevices));
            messages.Add(Upsert(command.UserComputers));
            messages.Add(Upsert(command.UserInstances));
        }

        public static void Select(CloudMessage command, CloudMessage results, List<string> messages, out string error)
        {
            var computerId = CloudHelper.GetComputerId(command, out error).Value;
            // Get all user games.
            if (command.UserGames != null)
            {
                UserGame[] userGames;
                error = Select(computerId, command.UserGames, out userGames);
                messages.Add(error);
                results.UserGames = FilterByChecksum(userGames, command.Checksums, out error);
                if (!string.IsNullOrEmpty(error))
                    messages.Add(error);
            }
            // Get all user devices.
            if (command.UserDevices != null)
            {
                UserDevice[] userDevices;
                error = Select(computerId, command.UserDevices, out userDevices);
                messages.Add(error);
                results.UserDevices = FilterByChecksum(userDevices, command.Checksums, out error);
                if (!string.IsNullOrEmpty(error))
                    messages.Add(error);
            }
            // Get all user computers.
            if (command.UserComputers != null)
            {
                UserComputer[] userComputers;
                error = Select(computerId, command.UserComputers, out userComputers);
                messages.Add(error);
                results.UserComputers = FilterByChecksum(userComputers, command.Checksums, out error);
                if (!string.IsNullOrEmpty(error))
                    messages.Add(error);
            }
            // Get all user instances.
            if (command.UserInstances != null)
            {
                UserInstance[] userInstances;
                error = Select(computerId, command.UserInstances, out userInstances);
                messages.Add(error);
                results.UserInstances = FilterByChecksum(userInstances, command.Checksums, out error);
                if (!string.IsNullOrEmpty(error))
                    messages.Add(error);
            }
        }

        public static void Delete(CloudMessage command, List<string> messages)
        {
            messages.Add(Delete(command.UserGames));
            messages.Add(Delete(command.UserDevices));
            messages.Add(Delete(command.UserComputers));
            messages.Add(Delete(command.UserInstances));
        }

        /// <summary>
        /// Remove all unchanged items to save network bandwith.
        /// </summary>
        public static T[] FilterByChecksum<T>(T[] items, Guid[] checksums, out string error) where T : IChecksum
        {
            error = null;
            var list = new T[0];
            if (checksums == null || checksums.Length == 0 || items == null || items.Length == 0)
                return items;
            var currentChecksums = EngineHelper.UpdateChecksums<T>(items);
            //// If last checksum is same then records have not changed.
            //if (checksums.Last().Equals(currentChecksums.Last()))
            //{
            //    return list;
            //}
            // Get only different items.
            list = items.Where(x => !checksums.Contains(x.Checksum)).ToArray();
            error = string.Format("{0} record(s) changed", list.Length);
            return list;
        }

        public static Guid? FixComputerId(CloudMessage command, out string error)
        {
            var computerId = CloudHelper.GetComputerId(command, out error);
            if (computerId.HasValue)
            {
                // Fix computer id
                if (command.UserGames != null)
                {
                    foreach (var item in command.UserGames)
                        item.ComputerId = computerId.Value;
                }
                if (command.UserDevices != null)
                {
                    foreach (var item in command.UserDevices)
                        item.ComputerId = computerId.Value;
                }
            }
            return computerId;
        }


    }
}
