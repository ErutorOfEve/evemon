﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace EVEMon.SDEExternalsToSql.SQLiteToSql.Tables
{
    internal static class MapCelestialStatisticsTable
    {
        private static int s_total;
        private const string TableName = "mapCelestialStatistics";

        /// <summary>
        /// Imports data in table of specified connection.
        /// </summary>
        public static void Import()
        {
            DateTime startTime = DateTime.Now;
            Util.ResetCounters();

            try
            {
                s_total = Database.UniverseDataContext.mapCelestialStatistics.Count();
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e.InnerException.Message);
                return;
            }

            Console.WriteLine();
            Console.Write(@"Importing {0}... ", TableName);

            Database.CreateTable(TableName);

            ImportData();

            Util.DisplayEndTime(startTime);

            Console.WriteLine();
        }

        /// <summary>
        /// Imports the data.
        /// </summary>
        private static void ImportData()
        {
            SqlCommand command = new SqlCommand { Connection = Database.SqlConnection };

            using (var tx = Database.SqlConnection.BeginTransaction())
            {
                command.Transaction = tx;
                try
                {
                    foreach (mapCelestialStatistics mCelStat in Database.UniverseDataContext.mapCelestialStatistics)
                    {
                        Util.UpdatePercentDone(s_total);

                        Dictionary<string, string> parameters = new Dictionary<string, string>();
                        parameters["celestialID"] = mCelStat.celestialID.ToString(CultureInfo.InvariantCulture);
                        parameters["temperature"] = mCelStat.temperature.GetValueOrDefaultString();
                        parameters["spectralClass"] = mCelStat.spectralClass.GetTextOrDefaultString();
                        parameters["luminosity"] = mCelStat.luminosity.GetValueOrDefaultString();
                        parameters["age"] = mCelStat.age.GetValueOrDefaultString();
                        parameters["life"] = mCelStat.life.GetValueOrDefaultString();
                        parameters["orbitRadius"] = mCelStat.orbitRadius.GetValueOrDefaultString();
                        parameters["eccentricity"] = mCelStat.eccentricity.GetValueOrDefaultString();
                        parameters["massDust"] = mCelStat.massDust.GetValueOrDefaultString();
                        parameters["massGas"] = mCelStat.massGas.GetValueOrDefaultString();
                        parameters["fragmented"] = mCelStat.fragmented.GetValueOrDefaultString();
                        parameters["density"] = mCelStat.density.GetValueOrDefaultString();
                        parameters["surfaceGravity"] = mCelStat.surfaceGravity.GetValueOrDefaultString();
                        parameters["escapeVelocity"] = mCelStat.escapeVelocity.GetValueOrDefaultString();
                        parameters["orbitPeriod"] = mCelStat.orbitPeriod.GetValueOrDefaultString();
                        parameters["rotationRate"] = mCelStat.rotationRate.GetValueOrDefaultString();
                        parameters["locked"] = mCelStat.locked.GetValueOrDefaultString();
                        parameters["pressure"] = mCelStat.pressure.GetValueOrDefaultString();
                        parameters["radius"] = mCelStat.radius.GetValueOrDefaultString();
                        parameters["mass"] = mCelStat.mass.GetValueOrDefaultString();

                        command.CommandText = Database.SqlInsertCommandText(TableName, parameters);
                        command.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
                catch (SqlException e)
                {
                    tx.Rollback();
                    Console.WriteLine();
                    Console.WriteLine(@"Unable to execute SQL command: {0}", command.CommandText);
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    Environment.Exit(-1);
                }
            }
        }
    }
}