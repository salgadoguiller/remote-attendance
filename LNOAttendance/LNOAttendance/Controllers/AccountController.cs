using System;
using System.Web.Mvc;
using System.Collections.Generic;
using LNOAttendance.Models;

namespace LNOAttendance.Controllers
{
    // ------------------------------------------------------------------------------
    // Funcion: esta clase se encarga de brindar toda la funcionalidad relacionada con
    //          la seguridad del sistema.
    // Propiedades: no tiene ninguna propiedad.
    // Metodos:
    //          index (GET)
    //          login (POST)
    //          logOut (GET)
    //          errorLogin
    //          redirectMain
    //          userValidated
    // ------------------------------------------------------------------------------

    public class AccountController : Controller
    {
        // ------------------------------------------------------------------------------
        // GET: /Account/index
        //      /Account/index?typeMsg=2&msg=message
        // Funcion: accion encargada de mostrar la vista de inicio de sesión en el sistema.
        // Parametros:
        //              typeMsg -> representa el tipo de mensaje de retroalimentación que se mostrara 
        //                          al usuario dependiendo del escenario que se presente.
        //              msg     -> contiene el mensaje que se mostrara al usuario.
        // ------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult index(int typeMsg = 0, string msg = "")
        {
            if (userValidated())
            {
                return RedirectToRoute(new
                {
                    controller = "Home",
                    action = "main",
                });
            }

            ViewData["typeMsg"] = typeMsg;
            ViewData["msg"] = msg;

            return View();
        }


        // ------------------------------------------------------------------------------
        // POST: /Account/index
        // Funcion: accion encargada de validar las credenciales de un usuario y permitir o no el acceso al sistema.
        // Parametros:
        //              username    -> nombre de usuario de dominio para acceder al sistema.
        //              password    -> contraseña de dominio para ingresar al sistema.
        // ------------------------------------------------------------------------------
        [HttpPost]
        public ActionResult login(string username, string password)
        {
            if (userValidated())
            {
                return RedirectToRoute(new
                {
                    controller = "Home",
                    action = "main",
                });
            }

            int typeMsg = 0;
            string msg = "";

            // Active directory
            ADConnection ad = new ADConnection();

            // Base de datos sistema de asistencia.
            DataBase dbc = new DataBase();

            string[] userInfo = {};

            // Validando usuario.
            try
            {
                userInfo = ad.IsAuthenticated(username, password);

                if (userInfo[1] == "")
                {
                    throw new Exception("No email found in AD.");
                }

                Session["userName"] = userInfo[0];
                Session["email"] = userInfo[1];
                Session["validated"] = "1";
            }
            catch (Exception ex)
            {
                typeMsg = 2;
                msg = "Error! Username or password incorrect.";
                Session.RemoveAll();
                return RedirectToRoute(new
                {
                    controller = "Account",
                    action = "index",
                    typeMsg = typeMsg,
                    msg = msg,
                });
            }

            // Asignando permisos.
            try
            {
                dbc.DBConnection();
            }
            catch (Exception ex)
            {
                typeMsg = 2;
                msg = "Error! Please try again or contact the support team.";
                Session.RemoveAll();
                return RedirectToRoute(new
                {
                    controller = "Account",
                    action = "index",
                    typeMsg = typeMsg,
                    msg = msg,
                });
            }

            try
            {
                Session["role"] = dbc.getRole(Convert.ToString(Session["email"]));
            }
            catch (Exception ex)
            {
                typeMsg = 2;
                msg = "Error! Please try again or contact the support team.";
                Session.RemoveAll();
                return RedirectToRoute(new
                {
                    controller = "Account",
                    action = "index",
                    typeMsg = typeMsg,
                    msg = msg,
                });
            }

            return RedirectToRoute(new
            {
                controller = "Home",
                action = "main",
            });
        }


        // ------------------------------------------------------------------------------
        // GET: /Account/logOut
        // Funcion: accion encargada de cerrar la sesion de un usuario.
        // Parametros: ninguno.
        // ------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult logOut()
        {
            Session.RemoveAll();
            return RedirectToRoute(new
            {
                controller = "Account",
                action = "index",
            });
        }


        // ------------------------------------------------------------------------------
        // Funcion: accion encargada de tomar la accion adecuada cuando existe un error en el inicio de sesion.
        //          cerrar la sesion y redirigir a /Account/index
        // Parametros:
        //              typeMsg -> representa el tipo de mensaje de retroalimentación que se mostrara 
        //                          al usuario dependiendo del escenario que se presente.
        //              msg     -> contiene el mensaje que se mostrara al usuario.
        // ------------------------------------------------------------------------------
        private ActionResult errorLogin(int typeMsg, string msg)
        {
            Session.RemoveAll();
            return RedirectToRoute(new
            {
                controller = "Account",
                action = "index",
                typeMsg = typeMsg,
                msg = msg,
            });
        }


        // ------------------------------------------------------------------------------
        // Funcion: accion encargada de redirigir a /Home/main
        // Parametros: ninguno.
        // ------------------------------------------------------------------------------
        private ActionResult redirectMain()
        {
            return RedirectToRoute(new
            {
                controller = "Home",
                action = "main",
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

    }
}