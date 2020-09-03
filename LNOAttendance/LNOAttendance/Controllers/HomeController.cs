using System;
using System.Web.Mvc;
using System.Collections.Generic;
using LNOAttendance.Models;

namespace LNOAttendance.Controllers
{
    // ------------------------------------------------------------------------------
    // Funcion: esta clase se encarga de brindar toda la funcionalidad a los empleados logeados
    //          Empleado Marcaje Remoto -> Marcaje Remoto
    //          Administrador           -> Marcaje Remoto
    //                                     Asignar Permisos Marcaje
    //                                     Remover Permisos Marcaje
    //          Super Administrador     -> Marcaje Remoto
    //                                     Asignar Permisos Marcaje
    //                                     Remover Permisos Marcaje
    //                                     Asignar Permisos Administrador
    //                                     Remover Permisos Administrador
    //                                     Asignar Permisos Super Administrador
    //                                     Remover Permisos Super Administrador
    // Propiedades: no tiene ninguna propiedad.
    // Metodos:
    //          main (GET)
    //          checkAttendance (POST)
    //          addPermissions (GET, POST)
    //          removePermissions (GET, POST)
    //          redirectLogin
    //          userValidated
    //          isAdmin
    //          isSuperAdmin
    //          addPermission (*3, sobrecargado)
    // ------------------------------------------------------------------------------
    public class HomeController : Controller
    {
        // ------------------------------------------------------------------------------
        // GET: /Home/main
        //      /Home/main?typeMsg=2&msg=message
        // Funcion: accion encargada de mostrar la vista principal de los Empleados logeados (Marcaje).
        // Parametros:
        //              typeMsg -> representa el tipo de mensaje de retroalimentación que se mostrara 
        //                          al usuario dependiendo del escenario que se presente.
        //              msg     -> contiene el mensaje que se mostrara al usuario.
        // ------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult main(int typeMsg = 0, string msg = "")
        {
            if (!userValidated())
            {
                return RedirectToRoute(new
                {
                    controller = "Account",
                    action = "index",
                    typeMsg = 2,
                    msg = "Please log in."
                });
            }

            ViewData["userName"] = Session["userName"];
            ViewData["email"] = Session["email"];
            ViewData["role"] = Session["role"];
            ViewData["typeMsg"] = typeMsg;
            ViewData["msg"] = msg;

            return View();
        }


        // ------------------------------------------------------------------------------
        // POST: /Account/checkAttendance
        // Funcion: accion encargada de marcar asistencia a Empleados autorizados.
        // Parametros:
        //              userName    -> nombre de usuario del empleado que realiza el marcaje remoto.
        //              email       -> correo electronico del empleado que realiza el marcaje remoto.
        //              option      -> representa si se marca entrada o salida.
        // ------------------------------------------------------------------------------
        [HttpPost]
        public ActionResult checkAttendance(string userName, string email, string option)
        {
            if (!userValidated())
            {
                return RedirectToRoute(new
                {
                    controller = "Account",
                    action = "index",
                    typeMsg = 2,
                    msg = "Please log in."
                });
            }

            DataBase dbc = new DataBase();

            string usrID = "";
            int typeMsg = 0;
            string msg = "";

            try
            {
                dbc.DBConnection();
            }
            catch (Exception ex)
            {
                typeMsg = 2;
                msg = "Error! Please try again or contact the support team.";
                // return View("Index");
            }

            try
            {
                usrID = dbc.getUserID(email);
            }
            catch (Exception ex)
            {
                typeMsg = 2;
                msg = "Error! Please try again or contact the support team.";
                // return View("Index");
            }

            if ((usrID == null) || (usrID == ""))
            {
                typeMsg = 2;
                msg = "Error! Please try again or contact the support team.";
                // return View("Index");
            }

            try
            {
                // ViewData["SQLInfo"] = dbc.check(usrID);
                msg = dbc.check2(usrID, option);
                typeMsg = 1;
            }
            catch (Exception ex)
            {
                typeMsg = 2;
                msg = "Error checking in/out. Please try again or contact the support team. ";
                // return View("Index");
            }

            dbc.DBDisconnect();

            return RedirectToRoute(new
            {
                controller = "Home",
                action = "main",
                typeMsg = typeMsg,
                msg = msg,
            });
        }


        // ------------------------------------------------------------------------------
        // GET: /Home/addPermissions
        //      /Home/addPermissions?typeMsg=2&msg=message
        // Funcion: accion encargada de mostrar la vista donde se asignan permisos a los empleados.
        //          Solo disponible para Administradores y Super Administradores.
        //          Administradores         -> asignar permisos de marcaje.
        //          Super Administradores   -> asignar permisos de marcaje, de administradores y de super administradores.
        // Parametros:
        //              typeMsg -> representa el tipo de mensaje de retroalimentación que se mostrara 
        //                          al usuario dependiendo del escenario que se presente.
        //              msg     -> contiene el mensaje que se mostrara al usuario.
        // ------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult addPermissions(int typeMsg = 0, string msg = "")
        {
            if (!userValidated())
            {
                return RedirectToRoute(new
                {
                    controller = "Account",
                    action = "index",
                    typeMsg = 2,
                    msg = "Please log in."
                });
            }

            if (!isAdmin() && !isSuperAdmin())
            {
                return RedirectToRoute(new
                {
                    controller = "Home",
                    action = "main",
                });
            }

            DataBase dbc = new DataBase();

            List<Employee> employees = new List<Employee>();

            try
            {
                dbc.DBConnection();
                employees = dbc.employees();
            }
            catch (Exception ex)
            {
                typeMsg = 2;
                msg += "Error: " + ex.Message;
            }


            ViewData["employees"] = employees;
            ViewData["userName"] = Session["userName"];
            ViewData["email"] = Session["email"];
            ViewData["role"] = Session["role"];
            ViewData["typeMsg"] = typeMsg;
            ViewData["msg"] = msg;

            if (isAdmin())
            {
                return View("addPermissionsA");
            }

            return View("addPermissionsSA");
        }


        // ------------------------------------------------------------------------------
        // POST: /Account/addPermissions
        // Funcion: accion encargada de agregar permisos a Empleados.
        // Parametros:
        //              type        -> tipo de permiso que se va a asignar (Marcaje, Administrador, Super Administrador)
        //              listItem    -> lista de empleados a los que se les asignara el permiso.
        //              daysOpt     -> representa la opción en cuanto a tiempo seleccionada:
        //                              one     -> permiso por un dia.
        //                              range   -> permiso por un rango de tiempo.
        //                              allways -> permiso por tiempo indefinido.
        //              startDate   -> parametro opcional que representa la fecha inicial del permiso.
        //                             cuando daysOpt = allways este parametro es null.
        //              endDate     -> parametro opcional que representa la fecha final del permiso.
        //                             cuando daysOpt = allways o daysOpt = one este parametro es null.
        // ------------------------------------------------------------------------------
        [HttpPost]
        public ActionResult addPermissions(string type, string[] listItem, string daysOpt, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (!userValidated())
            {
                return RedirectToRoute(new
                {
                    controller = "Account",
                    action = "index",
                    typeMsg = 2,
                    msg = "Please log in."
                });
            }

            if (!isAdmin() && !isSuperAdmin())
            {
                return RedirectToRoute(new
                {
                    controller = "Home",
                    action = "main",
                });
            }

            int typeMsg = 0;
            string msg = "";

            DataBase dbc = new DataBase();

            try
            {
                dbc.DBConnection();
                
                // Add Permissions
                foreach (string email in listItem)
                {
                    string role = dbc.getRole(email);
                    if (role == "Super Admin")
                    {
                        continue;
                    }
                    if (role == "Admin" && type != "Super Admin")
                    {
                        continue;
                    }

                    switch (daysOpt)
                    {
                        case "one":
                            dbc.addPermission(email, type, Convert.ToDateTime(startDate));
                            break;
                        case "range":
                            dbc.addPermission(email, type, Convert.ToDateTime(startDate), Convert.ToDateTime(endDate));
                            break;
                        case "allways":
                            dbc.addPermission(email, type);
                            break;
                    }
                }
                    
                typeMsg = 1;
                msg = "Success: Permission added successfully.";
            }
            catch (Exception ex)
            {
                typeMsg = 2;
                msg = "Error! Please try again or contact the support team.";
            }

            ViewData["userName"] = Session["userName"];
            ViewData["email"] = Session["email"];
            ViewData["typeMsg"] = typeMsg;
            ViewData["msg"] = msg;

            return RedirectToRoute(new
            {
                controller = "Home",
                action = "addPermissions",
                typeMsg = typeMsg,
                msg = msg,
            });
        }


        // ------------------------------------------------------------------------------
        // GET: /Home/removePermissions
        //      /Home/removePermissions?typeMsg=2&msg=message
        // Funcion: accion encargada de mostrar la vista donde se remueven permisos a los empleados.
        //          Solo disponible para Administradores y Super Administradores.
        //          Administradores         -> remover permisos de marcaje.
        //          Super Administradores   -> remover permisos de marcaje, de administradores y de super administradores.
        // Parametros:
        //              typeMsg -> representa el tipo de mensaje de retroalimentación que se mostrara 
        //                          al usuario dependiendo del escenario que se presente.
        //              msg     -> contiene el mensaje que se mostrara al usuario.
        // ------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult removePermissions(int typeMsg = 0, string msg = "")
        {
            if (!userValidated())
            {
                return RedirectToRoute(new
                {
                    controller = "Account",
                    action = "index",
                    typeMsg = 2,
                    msg = "Please log in."
                });
            }

            if (!isAdmin() && !isSuperAdmin())
            {
                return RedirectToRoute(new
                {
                    controller = "Home",
                    action = "main",
                });
            }

            DataBase dbc = new DataBase();

            List<Employee> employees = new List<Employee>();

            try
            {
                dbc.DBConnection();
                employees = dbc.employeesPermissions();
            }
            catch (Exception ex)
            {
                typeMsg = 2;
                msg += " Error: " + ex.Message;
            }

            ViewData["userName"] = Session["userName"];
            ViewData["email"] = Session["email"];
            ViewData["role"] = Session["role"];
            ViewData["typeMsg"] = typeMsg;
            ViewData["msg"] = msg;
            ViewData["employees"] = employees;

            if (isAdmin())
            {
                return View("removePermissionsA");
            }

            return View("removePermissionsSA");
        }


        // ------------------------------------------------------------------------------
        // POST: /Account/removePermissions
        // Funcion: accion encargada de remover permisos a Empleados.
        // Parametros:
        //              type        -> tipo de permiso que se va a remover (Marcaje, Administrador, Super Administrador)
        //              listItem    -> lista de empleados a los que se les removera el permiso.
        // ------------------------------------------------------------------------------
        [HttpPost]
        public ActionResult removePermissions(string type, string[] listItem)
        {
            if (!userValidated())
            {
                return RedirectToRoute(new
                {
                    controller = "Account",
                    action = "index",
                    typeMsg = 2,
                    msg = "Please log in."
                });
            }

            if (!isAdmin() && !isSuperAdmin())
            {
                return RedirectToRoute(new
                {
                    controller = "Home",
                    action = "main",
                });
            }

            int typeMsg = 0;
            string msg = "";


            DataBase dbc = new DataBase();

            try
            {
                dbc.DBConnection();
                string msg2 = "";
                foreach (string email in listItem)
                {
                    if(email == Convert.ToString(Session["email"]))
                    {
                        msg2 = " But Your account was not removed. You can not delete your own account.";
                        continue;
                    }
                    dbc.removePermission(email);
                }
                typeMsg = 1;
                msg = "Success: Permission removed successfully." + msg2;
            }
            catch (Exception ex)
            {
                typeMsg = 2;
                msg = "Error! Please try again or contact the support team.";
            }

            ViewData["userName"] = Session["userName"];
            ViewData["email"] = Session["email"];
            ViewData["typeMsg"] = typeMsg;
            ViewData["msg"] = msg;


            return RedirectToRoute(new
            {
                controller = "Home",
                action = "removePermissions",
                typeMsg = typeMsg,
                msg = msg,
            });
        }


        // ------------------------------------------------------------------------------
        // Funcion: accion encargada de redirigir a /Account/index
        // Parametros: ninguno.
        // ------------------------------------------------------------------------------
        private ActionResult redirectLogin()
        {
            return RedirectToRoute(new
            {
                controller = "Account",
                action = "index",
                typeMsg = 2,
                msg = "Please log in."
            });
        }


        // ------------------------------------------------------------------------------
        // Funcion: accion encargada de verificar si un empleado ya esta logeado.
        // Parametros: ninguno.
        // Retorno:
        //          bool    -> true : empleado validado.
        //                     false: empleado no validado.
        // ------------------------------------------------------------------------------
        private bool userValidated()
        {
            string validated = "";

            try
            {
                validated = Convert.ToString(Session["validated"]);
                if (validated == "1")
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception)
            {
                return false;
            }
        }


        // ------------------------------------------------------------------------------
        // Funcion: accion encargada de verificar si un empleado esta logeado como Administrador
        // Parametros: ninguno.
        // Retorno:
        //          bool    -> true : es Administrador.
        //                     false: no es Administrador.
        // ------------------------------------------------------------------------------
        private bool isAdmin()
        {
            bool resp = false;
            if(Convert.ToString(Session["role"]) == "Admin")
            {
                resp = true;
            }

            return resp;
        }


        // ------------------------------------------------------------------------------
        // Funcion: accion encargada de verificar si un empleado esta logeado como Super Administrador
        // Parametros: ninguno.
        // Retorno:
        //          bool    -> true : es Super Administrador.
        //                     false: no es Super Administrador.
        // ------------------------------------------------------------------------------
        private bool isSuperAdmin()
        {
            bool resp = false;
            if (Convert.ToString(Session["role"]) == "Super Admin")
            {
                resp = true;
            }

            return resp;
        }


        // ------------------------------------------------------------------------------
        // Funcion: accion encargada de agregar permisos de tiempo indefinido a Empleados.
        // Parametros:
        //              type    -> tipo de permiso que se va a asignar (Marcaje, Administrador, Super Administrador)
        //              email   -> email del empleados al que se le asignara el permiso.
        // ------------------------------------------------------------------------------
        private string addPermission(string email, string type)
        {
            DataBase dbc = new DataBase();

            try
            {
                dbc.DBConnection();
            }
            catch (Exception ex)
            {
                return "Error connecting to the database: " + ex.Message;
            }

            try
            {
                // Add Permission
                dbc.addPermission(email, type);
                return "Success: Permission added successfully.";
            }
            catch (Exception ex)
            {
                return "Error adding Permission: " + ex.Message;
            }
        }


        // ------------------------------------------------------------------------------
        // Funcion: accion encargada de agregar permisos de un dia a Empleados.
        // Parametros:
        //              type        -> tipo de permiso que se va a asignar (Marcaje, Administrador, Super Administrador)
        //              email       -> email del empleados al que se le asignara el permiso.
        //              startDate   -> dia que el empleado tendra permisos dentro del sistema.
        // ------------------------------------------------------------------------------
        private string addPermission(string email, string type, DateTime startDate)
        {
            DataBase dbc = new DataBase();

            try
            {
                dbc.DBConnection();
            }
            catch (Exception ex)
            {
                return  "Error connecting to the database: " + ex.Message;
            }

            try
            {
                // Add Permission
                dbc.addPermission(email, type, startDate);
                return "Success: Permission added successfully.";
            }
            catch (Exception ex)
            {
                return "Error adding Permission: " + ex.Message;
            }
        }


        // ------------------------------------------------------------------------------
        // Funcion: accion encargada de agregar permisos de un rango de tiempo a Empleados.
        // Parametros:
        //              type        -> tipo de permiso que se va a asignar (Marcaje, Administrador, Super Administrador)
        //              email       -> email del empleados al que se le asignara el permiso.
        //              startDate   -> dia de inicio del rango de tiempo que el empleado tendra permisos dentro del sistema.
        //              startDate   -> dia de finalizacion del rango de tiempo que el empleado tendra permisos dentro del sistema.
        // ------------------------------------------------------------------------------
        private string addPermission(string email, string type, DateTime startDate, DateTime endDate)
        {
            DataBase dbc = new DataBase();

            try
            {
                dbc.DBConnection();
            }
            catch (Exception ex)
            {
                return "Error connecting to the database: " + ex.Message;
            }

            try
            {
                // Add Permission
                dbc.addPermission(email, type, startDate, endDate);
                return "Success: Permission added successfully.";
            }
            catch (Exception ex)
            {
                return "Error adding Permission: " + ex.Message;
            }
        }
    }
}