using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace Emmares4.Helpers
{
    public class DBConnection
    {
        private static string ConnectionString { get; set; }

        public static string Get()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new ApplicationException("Database connection not specified.");
            }
            return ConnectionString;
        }

        public static void Set(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }

    public class DBParameter
    {
        public static SqlParameter String(string name, string value)
        {
            var p = new SqlParameter(name, SqlDbType.VarChar);
            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                p.Value = value;
            }
            return p;
        }

        public static SqlParameter Text(string name, string value)
        {
            var p = new SqlParameter(name, SqlDbType.Text);
            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                p.Value = value;
            }
            return p;
        }

        public static SqlParameter DateTime(string name, DateTime? value)
        {
            var p = new SqlParameter(name, SqlDbType.DateTime);
            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                p.Value = value;
            }
            return p;
        }

        public static SqlParameter Boolean(string name, bool? value)
        {
            var p = new SqlParameter(name, SqlDbType.Bit);
            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                p.Value = (bool)value;
            }
            return p;
        }

        public static SqlParameter Double(string name, double? value)
        {
            var p = new SqlParameter(name, SqlDbType.Float);
            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                p.Value = (double)value;
            }
            return p;
        }

        public static SqlParameter Int(string name, int? value)
        {
            var p = new SqlParameter(name, SqlDbType.Int);
            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                p.Value = (int)value;
            }
            return p;
        }
    }

    public class DBTools
    {
        public static string NormalizeTop(string query)
        {
            var tokens = query.Split(' ');
            if (tokens.Length >= 3)
            {
                if (tokens[0].ToLower() == "select" &&
                    tokens[1].ToLower() == "top")
                {
                    var nTokens = new string[tokens.Length - 3];
                    Array.Copy(tokens, 3, nTokens, 0, tokens.Length - 3);
                    return "select " + string.Join(" ", nTokens);
                }
            }
            return query;
        }
    }

    public class DBReader<T>
    {
        public static List<T> Read(string query, Func<SqlDataReader, T> map, Action<SqlParameterCollection> setParams = null, int initialTimeout = 120)
        {
            var attempt = 5;
            var timeout = initialTimeout;
            while (true)
            {
                try
                {
                    using (var con = new SqlConnection(DBConnection.Get()))
                    {
                        con.Open();
                        using (var cmd = new SqlCommand(query, con))
                        {
                            cmd.CommandTimeout = timeout;
                            if (setParams != null)
                            {
                                setParams(cmd.Parameters);
                            }
                            var reader = cmd.ExecuteReader();
                            var list = new List<T>();
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    var mo = map(reader);
                                    if (mo != null)
                                    {
                                        list.Add(mo);
                                    }
                                }
                            }
                            return list;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Contains("timeout"))
                    {
                        Thread.Sleep(15000);
                        if (attempt-- > 0)
                        {
                            timeout *= 2;
                            continue;
                        }
                    }
                    throw;
                }
            }
        }
    }

    public class DBWriter
    {
        public static int Write(string query, Action<SqlParameterCollection> setParams = null, int initialTimeout = 120)
        {
            var attempt = 5;
            var timeout = initialTimeout;
            while (true)
            {
                try
                {
                    using (var con = new SqlConnection(DBConnection.Get()))
                    {
                        con.Open();
                        using (var cmd = new SqlCommand(query, con))
                        {
                            cmd.CommandTimeout = timeout;
                            if (setParams != null)
                            {
                                setParams(cmd.Parameters);
                            }
                            return cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Contains("timeout"))
                    {
                        Thread.Sleep(15000);
                        if (attempt-- > 0)
                        {
                            timeout *= 2;
                            continue;
                        }
                    }
                    throw;
                }
            }
        }
    }
}
