/* =====================================================================
   Prueba técnica - Bonos de desempeño
   Script 2: tabla Bonus. Requiere haber ejecutado 01_schema_seed.sql.
   ===================================================================== */

SET QUOTED_IDENTIFIER ON; 
GO
USE BonusTest;
GO

CREATE TABLE dbo.Bonus (
    BonusId        INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentCode CHAR(5)        NOT NULL,
    EmployeeNumber INT            NOT NULL,
    Period         CHAR(7)        NOT NULL,                         -- YYYY-MM
    BonusTypeId    INT            NOT NULL REFERENCES dbo.BonusType(BonusTypeId),
    BonusValue     DECIMAL(16,2)  NOT NULL,                         -- monto fijo o porcentaje
    ReasonCode     CHAR(6)        NOT NULL REFERENCES dbo.BonusReason(ReasonCode),
    MonthlySalary  DECIMAL(16,2)  NOT NULL,                         -- sueldo al registrar
    TaxRate        DECIMAL(5,2)   NOT NULL,                         -- retención al registrar
    GrossAmount    DECIMAL(16,2)  NOT NULL,
    TaxAmount      DECIMAL(16,2)  NOT NULL,
    NetAmount      DECIMAL(16,2)  NOT NULL,
    BonusStateCode CHAR(4)        NOT NULL DEFAULT '0201' REFERENCES dbo.State(StateCode),
    CreatedBy      VARCHAR(50)    NOT NULL,
    CreatedAt      DATETIME2(0)   NOT NULL DEFAULT GETDATE(),
    ModifiedBy     VARCHAR(50)    NULL,
    ModifiedAt     DATETIME2(0)   NULL,
    DeletedBy      VARCHAR(50)    NULL,
    DeletedAt      DATETIME2(0)   NULL,                             -- NULL = no eliminado

    CONSTRAINT FK_Bonus_Employee FOREIGN KEY (DepartmentCode, EmployeeNumber)
        REFERENCES dbo.Employee(DepartmentCode, EmployeeNumber),

    -- Periodo YYYY-MM con mes entre 01 y 12.
    CONSTRAINT CK_Bonus_Period CHECK (
        Period LIKE '[0-9][0-9][0-9][0-9]-[0-1][0-9]'
        AND SUBSTRING(Period, 6, 2) BETWEEN '01' AND '12'),

    -- Monto fijo (1): > 0 y <= sueldo. Porcentaje (2): > 0 y <= 100.
    CONSTRAINT CK_Bonus_BonusValue CHECK (
        BonusValue > 0
        AND (BonusTypeId <> 1 OR BonusValue <= MonthlySalary)
        AND (BonusTypeId <> 2 OR BonusValue <= 100)),

    -- El neto siempre cuadra con bruto - retención.
    CONSTRAINT CK_Bonus_Amounts CHECK (
        GrossAmount > 0
        AND TaxAmount >= 0
        AND NetAmount = GrossAmount - TaxAmount),

    -- La FK a State también aceptaría estados de empleado (01xx): solo se permiten estados de bono.
    CONSTRAINT CK_Bonus_BonusStateCode CHECK (BonusStateCode IN ('0201', '0202', '0203')),

    -- Quién y cuándo eliminó van siempre juntos: ambos NULL o ambos con valor.
    CONSTRAINT CK_Bonus_Deleted CHECK (
        (DeletedAt IS NULL AND DeletedBy IS NULL)
        OR (DeletedAt IS NOT NULL AND DeletedBy IS NOT NULL))
);
GO

CREATE UNIQUE INDEX UX_Bonus_Employee_Period_Active
    ON dbo.Bonus (DepartmentCode, EmployeeNumber, Period)
    WHERE DeletedAt IS NULL AND BonusStateCode IN ('0201', '0202');
GO
