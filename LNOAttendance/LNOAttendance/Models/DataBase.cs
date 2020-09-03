using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Collections.Generic;
using LNOAttendance.Models;

namespace LNOAttendance.Controllers
{
    // ------------------------------------------------------------------------------
    // Funcion: acceder a la base de datos que tiene los roles de cada empleado.
    // Propiedades:
    //          SqlConnection
    // Metodos: 
    //          DBConnection
    //          DBDisconnect
    //          getUserID
    //          check
    //          check2
    //          getRole
    //          getRoleId
    //          addPermission (*3, sobrecargado)
    //          employees
    //          employeesPermissions
    //          removePermission
    //          removeAllPermission
    //          getSuperAdmins
    // ------------------------------------------------------------------------------
    public class DataBase
    {
        // Conexion a la base de datos.
        SqlConnection conn = null;

        // ------------------------------------------------------------------------------
        // Funcion: establecer conexion con la base de datos. 
        // Parametros: ninguno.
        // Retorno:
        //          string  -> cadena de conexion con la base de datos.
        // ------------------------------------------------------------------------------
        public string DBConnection()
        {

            System.Configuration.ConnectionStringSettings connString = WebConfigurationManager.ConnectionStrings["AnvizDatabaseCS"];
            conn = new SqlConnection(connString.ConnectionString);

            return conn.ConnectionString;
        }


        // ------------------------------------------------------------------------------
        // Funcion: finalizar conexion con la base de datos. 
        // Parametros: ninguno. 
        // Retorno: ninguno.
        // ------------------------------------------------------------------------------
        public void DBDisconnect()
        {
            if (conn != null)
                conn.Close();

            conn = null;
        }


        // ------------------------------------------------------------------------------
        // Funcion: obtener el identificador de un Empleado partiendo de el email de este.
        // Parametros:  
        //          email   ->  correo electronico del empleado.
        // Retorno:
        //          string  ->  identificador del Empleado.
        // ------------------------------------------------------------------------------
        public string getUserID(string email)
        {
            // Validates if conn has a valid instance
            if (conn == null)
                return "";

            if (conn.State != ConnectionState.Open)
                conn.Open();

            string query = "select Userinfo.Userid from Userinfo where Userinfo.Address='" + email + "'";
            SqlCommand queryCommand = new SqlCommand(query, conn);

            SqlDataReader queryCommandReader = queryCommand.ExecuteReader();

            DataTable dt = new DataTable();

            dt.Load(queryCommandReader);

            if (dt.Rows.Count == 0)
            {
                throw new Exception("Ups! Are your user and email associated in AD and Anviz DB?");
            } else if(dt.Rows.Count != 1)
            {
                throw new Exception("Too many users with the same e-mail");
            }

            return dt.Rows[0][0] + "";
        }


        // ------------------------------------------------------------------------------
        // Funcion: realizar marcaje remoto de un empleado.
        // Parametros: 
        //              userID  -> identificador del empleado que esta realizando marcaje remoto.
        // Retorno:
        //              string  -> mensaje de retroalimentacion.
        // ------------------------------------------------------------------------------
        public string check(string userID)
        {
            if (conn == null)
                return "";

            if (conn.State != ConnectionState.Open)
                conn.Open();

            // Look for a check in in the current date
            string query = "SELECT Checkinout.* from Checkinout where (Checkinout.Userid='" + userID + "' and Checkinout.CheckType='I')";
            SqlCommand queryCommand = new SqlCommand(query, conn);

            SqlDataReader queryCommandReader = queryCommand.ExecuteReader();

            DataTable dt = new DataTable();

            dt.Load(queryCommandReader);

            DateTime tmp = DateTime.Now;

            // first check in ever
            if (dt.Rows.Count == 0)
            {
                query = "INSERT INTO Checkinout (Userid, CheckTime, CheckType, Sensorid) VALUES (@userID,@checkTime,@checkType,@sensorID)";
                queryCommand = new SqlCommand(query, conn);
                queryCommand.Parameters.AddWithValue("@userID", userID);
                queryCommand.Parameters.AddWithValue("@checkTime", tmp);
                queryCommand.Parameters.AddWithValue("@checkType", "I");
                queryCommand.Parameters.AddWithValue("@sensorID", "99");

                if (queryCommand.ExecuteNonQuery() == 0)
                {
                    throw new Exception("Error inserting check in into the database");
                }

                return "Cheked in at " + tmp.ToString("F");
            }

            // Search for a check in for today
            DateTime tmp2 = new DateTime();
            tmp2 = Convert.ToDateTime(dt.Rows[dt.Rows.Count - 1][2]);

            if (tmp.Date != tmp2.Date)
            {
                query = "INSERT INTO Checkinout (Userid, CheckTime, CheckType, Sensorid) VALUES (@userID,@checkTime,@checkType,@sensorID)";
                queryCommand = new SqlCommand(query, conn);
                queryCommand.Parameters.AddWithValue("@userID", userID);
                queryCommand.Parameters.AddWithValue("@checkTime", tmp);
                queryCommand.Parameters.AddWithValue("@checkType", "I");
                queryCommand.Parameters.AddWithValue("@sensorID", "99");

                if (queryCommand.ExecuteNonQuery() == 0)
                {
                    throw new Exception("Error inserting check in into the database");
                }

                return "Cheked in at " + tmp.ToString("F");
            }


            // New check out (infitine check outs per day)
            query = "INSERT INTO Checkinout (Userid, CheckTime, CheckType, Sensorid) VALUES (@userID,@checkTime,@checkType,@sensorID)";
            queryCommand = new SqlCommand(query, conn);
            queryCommand.Parameters.AddWithValue("@userID", userID);
            queryCommand.Parameters.AddWithValue("@checkTime", tmp);
            queryCommand.Parameters.AddWithValue("@checkType", "O");
            queryCommand.Parameters.AddWithValue("@sensorID", "99");

            if (queryCommand.ExecuteNonQuery() == 0)
            {
                throw new Exception("Error inserting check out into the database");
            }

            TimeSpan worked = tmp.Subtract(tmp2);

            string retVal = "Check out at " + tmp.ToString("F") + ". Total time registered today: " + worked.ToString(@"hh\:mm\:ss");

            if (worked.Hours > 0)
            {
                retVal += " hours.";
            } else if (worked.Minutes > 0)
            {
                retVal += " minutes.";
            } else if (worked.Seconds > 0)
            {
                retVal += " seconds.";
            }

            return retVal;
        }


        // ------------------------------------------------------------------------------
        // Funcion: realizar marcaje remoto de un empleado.
        // Parametros:
        //          userId  ->  identificador del empleado que esta realizando marcaje remoo.
        //          option  ->  tipo de marcaje:
        //                      in  ->  entrada
        //                      out ->  salida
        // Retorno:
        //          string  ->  mensaje de retroalimentacion.
        // ------------------------------------------------------------------------------
        public string check2(string userID, string option)
        {
            if (conn == null)
                return "";

            if (conn.State != ConnectionState.Open)
                conn.Open();

            DateTime dt = DateTime.Now;
            
            string query = "INSERT INTO Checkinout (Userid, CheckTime, CheckType, Sensorid) VALUES (@userID,@checkTime,@checkType,@sensorID)";
            SqlCommand queryCommand = new SqlCommand(query, conn);
            queryCommand.Parameters.AddWithValue("@userID", userID);
            queryCommand.Parameters.AddWithValue("@checkTime", dt);
            if (option == "in")
            {
                queryCommand.Parameters.AddWithValue("@checkType", "I");
            }
            else
            {
                queryCommand.Parameters.AddWithValue("@checkType", "O");
            }
            queryCommand.Parameters.AddWithValue("@sensorID", "99");

            if (queryCommand.ExecuteNonQuery() == 0)
            {
                throw new Exception("Error inserting check in into the database");
            }

            string retVal = "Cheked " + option + " at " + dt.ToString("F");

            // query = "SELECT Checkinout.* from Checkinout where (Checkinout.Userid='" + userID + "' and Checkinout.CheckType='I')";
            query= "select Checkinout.* " + 
                    "from Checkinout " +
                    "where Checkinout.Userid='" + userID + "'and cast(Checkinout.CheckTime as date) ='" + DateTime.Now.ToString("MM-dd-yyyy") + "' and  Checkinout.Sensorid = '99' " +
                    "order by Logid asc";
            queryCommand = new SqlCommand(query, conn);
            SqlDataReader queryCommandReader = queryCommand.ExecuteReader();
            DataTable data = new DataTable();
            data.Load(queryCommandReader);

            DateTime tmp2 = new DateTime();
            tmp2 = Convert.ToDateTime(data.Rows[0][2]);

            TimeSpan worked = dt.Subtract(tmp2);

            retVal = retVal + ". Total time registered today: " + worked.ToString(@"hh\:mm\:ss");

            if (worked.Hours > 0)
            {
                retVal += " hours.";
            }
            else if (worked.Minutes > 0)
            {
                retVal += " minutes.";
            }
            else if (worked.Seconds > 0)
            {
                retVal += " seconds.";
            }

            return retVal;
        }


        // ------------------------------------------------------------------------------
        // Funcion: obtener el rol de un empleado.
        // Parametros:
        //          email   ->  correo electronico del empleado.
        // Retorno:
        //          string  ->  rol del empleado.
        // ------------------------------------------------------------------------------
        public string getRole(string email)
        {
            string role = "";

            // Validates if conn has a valid instance
            if (conn == null)
                throw new Exception();

            if (conn.State != ConnectionState.Open)
                conn.Open();

            // Obteniendo identificador del empleado.
            string userId = this.getUserID(email);

            string query = "select Roles.RoleName, EmployeesPermissions.StartDate, EmployeesPermissions.EndDate " +
                           "from Roles inner join EmployeesPermissions on Roles.RoleId = EmployeesPermissions.RoleId " +
                           "where EmployeesPermissions.UserId='" + userId + "'";
            SqlCommand queryCommand = new SqlCommand(query, conn);

            SqlDataReader reader = queryCommand.ExecuteReader();

            if (reader.Read())
            {
                // Permiso 'Allways'
                if (Convert.ToString(reader.GetSqlValue(1)) == "Null")
                {
                    role = reader.GetString(0);
                }
                else
                {
                    // Permiso 'range'
                    if (Convert.ToString(reader.GetSqlValue(1)) != Convert.ToString(reader.GetSqlValue(2)) &&

                        ((reader.GetDateTime(1).Year < DateTime.Now.Year) ||
                        (reader.GetDateTime(1).Year == DateTime.Now.Year && reader.GetDateTime(1).Month < DateTime.Now.Month) ||
                        (reader.GetDateTime(1).Year == DateTime.Now.Year && reader.GetDateTime(1).Month == DateTime.Now.Month && reader.GetDateTime(1).Day <= DateTime.Now.Day)) &&

                        ((reader.GetDateTime(2).Year > DateTime.Now.Year) ||
                        (reader.GetDateTime(2).Year == DateTime.Now.Year && reader.GetDateTime(2).Month > DateTime.Now.Month) ||
                        reader.GetDateTime(2).Year == DateTime.Now.Year && reader.GetDateTime(2).Month == DateTime.Now.Month && reader.GetDateTime(2).Day >= DateTime.Now.Day))
                    {
                        role = reader.GetString(0);
                    }
                    else
                    {
                        // Permiso 'day'
                        if (Convert.ToString(reader.GetSqlValue(1)) == Convert.ToString(reader.GetSqlValue(2)) &&
                            reader.GetDateTime(1).Year == DateTime.Now.Year && reader.GetDateTime(1).Month == DateTime.Now.Month && reader.GetDateTime(1).Day == DateTime.Now.Day)
                        {
                            role = reader.GetString(0);
                        }
                        else
                        {
                            role = "Any";
                        }
                    }
                }
            }
            else
            {
                role = "Any";
            }

            return role;
        }


        // ------------------------------------------------------------------------------
        // Funcion: obtener el identificador de un rol.
        // Parametros:
        //          type    ->  nombre del rol.
        // Retorno:
        //          string  ->  identificador del rol.
        // ------------------------------------------------------------------------------
        public string getRoleId(string type)
        {
            string roleId = "";

            // Validates if conn has a valid instance
            if (conn == null)
                throw new Exception();

            if (conn.State != ConnectionState.Open)
                conn.Open();

            string query = "select Roles.RoleId " +
                           "from Roles " +
                           "where Roles.RoleName='" + type + "'";
            SqlCommand queryCommand = new SqlCommand(query, conn);

            SqlDataReader reader = queryCommand.ExecuteReader();

            if (reader.Read())
            {
                roleId = Convert.ToString(reader.GetInt32(0));
            }

            return roleId;
        }


        // ------------------------------------------------------------------------------
        // Funcion: agregar permisos de tiempo indefinido a Empleados.
        // Parametros:
        //              type    -> tipo de permiso que se va a asignar (Marcaje, Administrador, Super Administrador)
        //              email   -> email del empleados al que se le asignara el permiso.
        // Retorno: ninguno.
        // ------------------------------------------------------------------------------
        public void addPermission(string email, string type)
        {
            // Validates if conn has a valid instance
            if (conn == null)
                return;

            if (conn.State != ConnectionState.Open)
                conn.Open();

            removePermission(email);

            // Obteniendo identificador del empleado.
            string userId = this.getUserID(email);

            // Obteniendo el identificador del rol que se asignara.
            string roleId = this.getRoleId(type);

            string query = "insert into EmployeesPermissions " +
                            "values ("
                                       + userId + "," 
                                       + roleId + "," 
                                       + "null" + "," 
                                       + "null" + 
                                    ")";

            SqlCommand queryCommand = new SqlCommand(query, conn);

            queryCommand.ExecuteNonQuery();

            return;
        }


        // ------------------------------------------------------------------------------
        // Funcion: accion encargada de agregar permisos de un dia a Empleados.
        // Parametros:
        //              type        -> tipo de permiso que se va a asignar (Marcaje, Administrador, Super Administrador)
        //              email       -> email del empleados al que se le asignara el permiso.
        //              startDate   -> dia que el empleado tendra permisos dentro del sistema.
        // Retorno: ninguno.
        // ------------------------------------------------------------------------------
        public void addPermission(string email, string type, DateTime startDate)
        {
            // Validates if conn has a valid instance
            if (conn == null)
                return;

            if (conn.State != ConnectionState.Open)
                conn.Open();

            removePermission(email);

            // Obteniendo identificador del empleado.
            string userId = this.getUserID(email);

            // Obteniendo el identificador del rol que se asignara.
            string roleId = this.getRoleId(type);

            string query = "insert into EmployeesPermissions " +
                            "values ("
                                       + userId + ","
                                       + roleId + ","
                                       + "'" + startDate.ToString("yyyy-MM-dd") + "',"
                                       + "'" + startDate.ToString("yyyy-MM-dd") + "'" +
                                    ")";

            SqlCommand queryCommand = new SqlCommand(query, conn);

            queryCommand.ExecuteNonQuery();

            return;
        }


        // ------------------------------------------------------------------------------
        // Funcion: accion encargada de agregar permisos de un rango de tiempo a Empleados.
        // Parametros:
        //              type        -> tipo de permiso que se va a asignar (Marcaje, Administrador, Super Administrador)
        //              email       -> email del empleados al que se le asignara el permiso.
        //              startDate   -> dia de inicio del rango de tiempo que el empleado tendra permisos dentro del sistema.
        //              startDate   -> dia de finalizacion del rango de tiempo que el empleado tendra permisos dentro del sistema.
        // Retorno: ninguno.
        // ------------------------------------------------------------------------------
        public void addPermission(string email, string type, DateTime startDate, DateTime endDate)
        {
            // Validates if conn has a valid instance
            if (conn == null)
                return;

            if (conn.State != ConnectionState.Open)
                conn.Open();

            removePermission(email);

            // Obteniendo identificador del empleado.
            string userId = this.getUserID(email);

            // Obteniendo el identificador del rol que se asignara.
            string roleId = this.getRoleId(type);

            string query = "insert into EmployeesPermissions " +
                            "values ("
                                       + userId + ","
                                       + roleId + ","
                                       + "'" + startDate.ToString("yyyy-MM-dd") + "',"
                                       + "'" + endDate.ToString("yyyy-MM-dd") + "'" +
                                    ")";

            SqlCommand queryCommand = new SqlCommand(query, conn);

            queryCommand.ExecuteNonQuery();

            return;
        }


        // ------------------------------------------------------------------------------
        // Funcion: retornar todos los empleados de LNO Honduras desde la base de datos del sistema de asistencia.
        // Parametros: ninguno.
        // Retorno:
        //          List<Employees> -> empleados LNO Honduras.
        // ------------------------------------------------------------------------------
        public List<Employee> employees()
        {
            List<Employee> employees = new List<Employee>();

            if (conn == null)
                throw new Exception("Error connecting to the database.");

            if (conn.State != ConnectionState.Open)
                conn.Open();

            string query = "select Userinfo.Name, Userinfo.Address " +
                            "from Userinfo " +
                            "where Userinfo.Name not like '' and " +
                            "Userinfo.Address not like '' " +
                            "order by Userinfo.Name asc";

            SqlCommand queryCommand = new SqlCommand(query, conn);

            SqlDataReader reader = queryCommand.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (Convert.ToString(reader.GetSqlValue(0)) != "Null")
                    {
                        if (Convert.ToString(reader.GetSqlValue(1)) != "Null")
                        {
                            Employee emp = new Employee(reader.GetString(0), reader.GetString(1));
                            employees.Add(emp);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Error getting employees.");
            }

            return employees;
        }


        // ------------------------------------------------------------------------------
        // Funcion: retornar todos los empleados de LNO Honduras que tiene un rol asignado (Marcaje, Administrador, Super Administrador)
        // Parametros: ninguno.
        // Retorno:
        //          List<Employees> -> empleados LNO Honduras con un rol asignado.
        // ------------------------------------------------------------------------------
        public List<Employee> employeesPermissions()
        {
            List<Employee> employees = new List<Employee>();

            if (conn == null)
                throw new Exception("Error connecting to the database.");

            if (conn.State != ConnectionState.Open)
                conn.Open();

            string query = "select Userinfo.Name, Userinfo.Address, EmployeesPermissions.RoleId, " +
                    "EmployeesPermissions.StartDate, EmployeesPermissions.EndDate " +
                    "from UserInfo inner join EmployeesPermissions " +
                    "on UserInfo.Userid = EmployeesPermissions.UserId " +
                    "order by Userinfo.Name asc";

            SqlCommand queryCommand = new SqlCommand(query, conn);

            SqlDataReader reader = queryCommand.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (Convert.ToString(reader.GetSqlValue(3)) != "Null")
                    {
                        Employee emp = new Employee(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetDateTime(3), reader.GetDateTime(4));
                        employees.Add(emp);
                    }
                    else
                    {
                        Employee emp = new Employee(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), null, null);
                        employees.Add(emp);
                    }
                }
            }
            else
            {
                throw new Exception("Error getting employees.");
            }

            return employees;
        }


        // ------------------------------------------------------------------------------
        // Funcion: remover permisos de empleado.
        // Parametros:
        //          email   ->  correo electronico del empleado (para obtener identificador).
        // Retorno: ninguno.
        // ------------------------------------------------------------------------------
        public void removePermission(string email)
        {
            // Validates if conn has a valid instance
            if (conn == null)
                return;

            if (conn.State != ConnectionState.Open)
                conn.Open();

            // Obteniendo identificador del empleado.
            string userId = this.getUserID(email);

            string query = "delete " +
                           "from EmployeesPermissions " +
                           "where EmployeesPermissions.UserId = " + userId;

            SqlCommand queryCommand = new SqlCommand(query, conn);

            queryCommand.ExecuteNonQuery();

            return;
        }


        // ------------------------------------------------------------------------------
        // Funcion: remover los permisos a todos los empleados de un rol especifico. 
        // Parametros: 
        //          type    ->  nombre del rol.
        // Retorno: ninguno.
        // ------------------------------------------------------------------------------
        public void removeAllPermission(string type)
        {
            // Validates if conn has a valid instance
            if (conn == null)
                return;

            if (conn.State != ConnectionState.Open)
                conn.Open();

            string query = "delete " +
                           "from EmployeesPermissions " +
                           "where EmployeesPermissions.RoleId = " + type;

            SqlCommand queryCommand = new SqlCommand(query, conn);

            queryCommand.ExecuteNonQuery();

            return;
        }


        // ------------------------------------------------------------------------------
        // Funcion: obtener la cantidad de Super Administradores del sistema.
        // Parametros: ninguno.
        // Retorno:
        //          int ->  cantidad de Super Administradores en el sistema.
        // ------------------------------------------------------------------------------
        public int getSuperAdmins()
        {
            int superAdmins = 0;

            // Validates if conn has a valid instance
            if (conn == null)
                throw new Exception();

            if (conn.State != ConnectionState.Open)
                conn.Open();

            string query = "select count(*) " +
                           "from EmployeesPermissions " +
                           "where EmployeesPermissions.RoleId='1'";

            SqlCommand queryCommand = new SqlCommand(query, conn);

            SqlDataReader reader = queryCommand.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    superAdmins = superAdmins + 1;
                }
            }

            return superAdmins;
        }
    }
}