using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Collections.Generic;
using System.Configuration;
namespace CleanDB
{
    // ------------------------------------------------------------------------------
    // Funcion: acceder a la base de datos que tiene los roles de cada empleado.
    // Propiedades:
    //          SqlConnection
    // Metodos: 
    //          DBConnection()
    //          DBDisconnect()
    //          clean()
    // ------------------------------------------------------------------------------
    public class DataBase
    {
        // conexion a la base de datos.
        SqlConnection conn = null;

        // Funcion: establecer conexion con la base de datos. 
        // Parametros: ninguno.
        // Retorno:
        //          string  -> cadena de conexion con la base de datos.
        public string DBConnection()
        {

            ConnectionStringSettings connString = ConfigurationManager.ConnectionStrings["AnvizDatabaseCS"];
            conn = new SqlConnection(connString.ConnectionString);

            return conn.ConnectionString;
        }

        // Funcion: finalizar conexion con la base de datos. 
        // Parametros: ninguno. 
        // Retorno: ninguno.
        public void DBDisconnect()
        {
            if (conn != null)
                conn.Close();

            conn = null;
        }

        // Funcion: eliminar permisos vencidos.
        // Parametros: ninguno. 
        // Retorno: ninguno.
        public void clean()
        {
            if (conn == null)
                return;

            if (conn.State != ConnectionState.Open)
                conn.Open();

            string query = "delete " +
                           "from EmployeesPermissions " +
                           "where EmployeesPermissions.EndDate < '" + DateTime.Now.ToString("yyyy-MM-dd") + "' ";
            
            SqlCommand queryCommand = new SqlCommand(query, conn);

            queryCommand.ExecuteNonQuery();

            return;
        }
    }
}