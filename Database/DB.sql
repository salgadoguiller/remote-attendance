use Anviz_Data_Base;
/*===============================================================================================*/
/*===============================================================================================*/
/* Tablas. */
/*===============================================================================================*/
/*===============================================================================================*/

/*-----------------------------------------------------------------------------------------------*/
/* Almacena todos los roles que soporta el sistema de asistencia remota. */
/*-----------------------------------------------------------------------------------------------*/
create table Roles(
	RoleId int identity(1,1) not null,	/* PK */
	RoleName varchar(50) not null,

	constraint PK_Roles primary key (RoleId),
);

/*-----------------------------------------------------------------------------------------------*/
/* Almacena los roles que tiene cada usuario dentro del sistema */
/*-----------------------------------------------------------------------------------------------*/
create table EmployeesPermissions(
	UserId int not null,
	RoleId int not null,
	
	/* Si StartDate y EndDate no son null: */
	/*		StartDate = EndDate  -> Permiso de un dia. */
	/*		StartDate != EndDate -> Permiso de un rango de tiempo. */
	/* Si StartDate y EndDate son null */
	/*		Permiso por un periodo de tiempo indeterminado */
	StartDate datetime,
	EndDate datetime,

	constraint PK_EmployeesPermision primary key (UserId, RoleId),
	constraint FK_Roles foreign key (RoleId) references Roles(RoleId)
);


/*===============================================================================================*/
/*===============================================================================================*/
/* Inserts. */
/*===============================================================================================*/
/*===============================================================================================*/
insert into Roles values ('Super Admin'),
						('Admin'),
						('Employee');

insert into EmployeesPermissions values (120, 1, null, null);