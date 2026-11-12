/* =====================================================================
   Prueba técnica - Bonos de desempeño
   Script 3: reparto del bono pagado por proyecto. Requiere 02_bonus.sql.
   ===================================================================== */

SET QUOTED_IDENTIFIER ON;
GO
USE BonusTest;
GO

CREATE TABLE dbo.BonusProjectDistribution (
    BonusProjectDistributionId INT IDENTITY(1,1) PRIMARY KEY,
    BonusId                    INT            NOT NULL REFERENCES dbo.Bonus(BonusId),
    EmployeeProjectHoursId     INT            NOT NULL REFERENCES dbo.EmployeeProjectHours(EmployeeProjectHoursId),
    ProjectName                VARCHAR(100)   NOT NULL,                 -- proyecto al pagar
    Hours                      DECIMAL(8,2)   NOT NULL,                 -- horas al pagar
    Amount                     DECIMAL(16,2)  NOT NULL,
    CreatedBy                  VARCHAR(50)    NOT NULL,
    CreatedAt                  DATETIME2(0)   NOT NULL DEFAULT GETDATE(),

    CONSTRAINT CK_BonusProjectDistribution_Hours CHECK (Hours > 0),
    CONSTRAINT CK_BonusProjectDistribution_Amount CHECK (Amount >= 0)
);
GO

-- Un proyecto aparece una sola vez por bono; también sirve para leer el detalle por BonusId.
CREATE UNIQUE INDEX UX_BonusProjectDistribution_Bonus_Project
    ON dbo.BonusProjectDistribution (BonusId, EmployeeProjectHoursId);
GO
