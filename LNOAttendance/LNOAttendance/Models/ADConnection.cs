using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using LNOAttendance.Models;

namespace LNOAttendance.Controllers
{
    // ------------------------------------------------------------------------------
    // Funcion: acceder al Active Directory para validar credenciales de empleados.
    // Propiedades:
    //          _path
    //          _filterAttribute
    // Metodos: 
    //          IsAuthenticated
    //          GetGroups
    //          employees
    // ------------------------------------------------------------------------------
    public class ADConnection
    {
        //
        private String _path = WebConfigurationManager.AppSettings["ADPath"];
        
        //
        private String _filterAttribute;


        // ------------------------------------------------------------------------------
        // Funcion: verificar credenciales de dominio de un empleado para iniciar sesion en el sistema.
        // Parametros:  
        //          username    ->  nombre de usuario de dominio del empleado.
        //          pwd         ->  contraseña de dominio del usuario.
        // Retorno:
        //          string[]    -> informacion del empleado:  
        //                      string[0] -> nombre del empleado.
        //                      string[1] -> email del empleado (para buscar identificador en el sistema de asistencia ANVIZ).
        // ------------------------------------------------------------------------------
        public string[] IsAuthenticated(String username, String pwd)
        {
            String domain = WebConfigurationManager.AppSettings["ADDomain"];

            String domainAndUsername = domain + @"\" + username;
            DirectoryEntry entry = new DirectoryEntry(_path, domainAndUsername, pwd);

            string[] retVals = new string[2];

            try
            {		
                Object obj = entry.NativeObject;
                DirectorySearcher search = new DirectorySearcher(entry);

                search.Filter = "(SAMAccountName=" + username + ")";
                search.PropertiesToLoad.Add("cn");
                search.PropertiesToLoad.Add("displayName");
                search.PropertiesToLoad.Add("mail");

                SearchResult result = search.FindOne();

                if (null == result)
                {
                    throw new Exception("Error authenticating user, no user found on AD");
                }

                string propName = "displayName";
                ResultPropertyValueCollection valColl = result.Properties[propName];
                retVals[0] = valColl[0].ToString();

                propName = "mail";
                valColl = result.Properties[propName];
                retVals[1] = valColl[0].ToString();

                _path = result.Path;
                _filterAttribute = (String)result.Properties["cn"][0];
            }
            catch (Exception ex)
            {
                throw new Exception("Error authenticating user. " + ex.Message);
            }

            return retVals;
        }


        // ------------------------------------------------------------------------------
        // Funcion:
        // Parametros: ninguno.
        // Retorno:
        //          String  ->  
        // ------------------------------------------------------------------------------
        public String GetGroups()
        {
            DirectorySearcher search = new DirectorySearcher(_path);
            search.Filter = "(cn=" + _filterAttribute + ")";
            search.PropertiesToLoad.Add("memberOf");
            StringBuilder groupNames = new StringBuilder();

            try
            {
                SearchResult result = search.FindOne();

                int propertyCount = result.Properties["memberOf"].Count;

                String dn;
                int equalsIndex, commaIndex;

                for (int propertyCounter = 0; propertyCounter < propertyCount; propertyCounter++)
                {
                    dn = (String)result.Properties["memberOf"][propertyCounter];

                    equalsIndex = dn.IndexOf("=", 1);
                    commaIndex = dn.IndexOf(",", 1);
                    if (-1 == equalsIndex)
                    {
                        return null;
                    }

                    groupNames.Append(dn.Substring((equalsIndex + 1), (commaIndex - equalsIndex) - 1));
                    groupNames.Append("|");

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error obtaining group names. " + ex.Message);
            }
            return groupNames.ToString();
        }


        // ------------------------------------------------------------------------------
        // Funcion: retornar todos los empleados de LNO Honduras desde el Active Directory.
        // Parametros: ninguno.
        // Retorno:
        //          List<Employees> -> empleados LNO Honduras.
        // * Este metodo se dejo de utilizar por el tiempo de carga de los empleados.
        // * En lugar de este se utiliza LNOAttendance/Models/DataBase.cs/employees que carga los empleados desde la base de datos del sistema de asistencia.
        // ------------------------------------------------------------------------------
        public List<Employee> employees()
        {
            List<Employee> employees = new List<Employee>();

            String domain = WebConfigurationManager.AppSettings["ADDomain"];

            DirectoryEntry entry = new DirectoryEntry(_path);

            try
            {   //Bind to the native AdsObject to force authentication.			

                Object obj = entry.NativeObject;
                DirectorySearcher search = new DirectorySearcher(entry);

                // search.Filter = "(SAMAccountName=" + username + ")";
                search.PropertiesToLoad.Add("cn");
                search.PropertiesToLoad.Add("displayName");
                search.PropertiesToLoad.Add("mail");

                SearchResultCollection result = search.FindAll();

                if (result == null)
                {
                    throw new Exception("Empty");
                }

                foreach (SearchResult res in result)
                {
                    string userName = "";
                    try
                    {
                        //get email information
                        string propName = "displayName";
                        ResultPropertyValueCollection valColl = res.Properties[propName];
                        userName = valColl[0].ToString();
                    }
                    catch (Exception e)
                    {
                        continue;
                    }

                    string email = "";
                    try
                    {
                        //get email information
                        string propName = "mail";
                        ResultPropertyValueCollection valColl = res.Properties[propName];
                        email = valColl[0].ToString();
                    }
                    catch (Exception e)
                    {
                        continue;
                    }

                    Employee emp = new Employee(userName, email);

                    employees.Add(emp);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return employees;
        }
    }
}