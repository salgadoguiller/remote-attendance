using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace CleanDB
{
    // ------------------------------------------------------------------------------
    // Funcion: limpiar roles asignados a empleados en caso de que la fecha de finalización
    //          haya pasado.
    // Propiedades: no tiene ninguna propiedad.
    // Metodos: 
    //          Main    -> ejecuta toda la funcionalidad de limpiar permisos vencidos.
    // ------------------------------------------------------------------------------
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Cleaning DB............");

            Thread.Sleep(2000);

            // Instancia de la clase DataBase que interactua directamente con la base de datos
            // que almacena los permisos.
            DataBase dbc = new DataBase();

            bool isClean = false;

            do
            {
                try
                {
                    // Conectando con la base de datos.
                    dbc.DBConnection();

                    // Ejecutando metodo que limpia los permisos.
                    dbc.clean();

                    // Permisos vencidos eliminados exitosamente, establecer isClean = true para salir del ciclo.
                    isClean = true;
                }
                catch (Exception ex)
                {
                    // Permisos vencidos no eliminados.
                    Console.WriteLine("Error. Retrying...");
                    Console.WriteLine(ex.Message);
                    Thread.Sleep(2000);

                    // establecer isClean = false para intentarlo de nuevo.
                    isClean = false;
                }
            } while (!isClean);

            Console.WriteLine("Success. Finalizing....");
            Thread.Sleep(2000);
        }
    }
}
