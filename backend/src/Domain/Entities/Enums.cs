namespace Domain.Entities;

// Un enum = lista fija de valores válidos. Como en Prisma: enum Rol { ADMIN AGENTE SOLICITANTE }
public enum Rol { Admin, Agente, Solicitante }

public enum Prioridad { Baja, Media, Alta, Critica }

public enum EstadoSolicitud { Nueva, Asignada, EnProceso, Resuelta, Cerrada, Cancelada }