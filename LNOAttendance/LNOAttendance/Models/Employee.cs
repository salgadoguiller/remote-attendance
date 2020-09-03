using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LNOAttendance.Models
{
    // ------------------------------------------------------------------------------
    // Funcion: esta clase permite almacenar la información de Empleado proveniente de la base de datos.
    // Propiedades:
    //          Id          -> identificador unico de un empleado.
    //          Name        -> nombre del empleado.
    //          Email       -> email del empleado.
    //          Role        -> rol del empleado dentro del sistema.
    //          StartDate   -> Fecha de inicio del permiso que tiene asignado dentro del sistema.
    //          EndDate     -> Fecha de finalizacion del permiso que tiene asignado dentro del sistema.
    //                          Si StartDate y EndDate no son null:
    //		                        StartDate = EndDate  -> Permiso de un dia.
    //		                        StartDate != EndDate -> Permiso de un rango de tiempo.
    //                          Si StartDate y EndDate son null 
    //		                        Permiso por un periodo de tiempo indeterminado
    // Metodos: constructores sobrecargados (*3).
    // ------------------------------------------------------------------------------
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Role { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }


        public Employee(string name, string email)
        {
            this.Name = name;
            this.Email = email;
        }


        public Employee(string name, string email, int role)
        {
            this.Name = name;
            this.Email = email;
            this.Role = role;
        }


        public Employee(string name, string email, int role, DateTime? startDate, DateTime? endDate)
        {
            this.Name = name;
            this.Email = email;
            this.Role = role;
            this.StartDate = startDate;
            this.EndDate = endDate;
        }
    }
}