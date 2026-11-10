/* Menu: "View Employee Documents" (route /dashboard/view-employee-documents), next to Employee Profile.
   ScreenName must stay exactly as below. ScreenCode EMPDOCVIEW is checked by the API ("V"). */
IF NOT EXISTS (SELECT 1 FROM PayrollScreens WHERE ScreenCode = 'EMPDOCVIEW')
BEGIN
    DECLARE @Parent INT = (SELECT TOP 1 IdParentPayrollScreen FROM PayrollScreens WHERE ScreenCode = 'EMPPROF');
    DECLARE @Ord INT = (SELECT TOP 1 OrderNumber FROM PayrollScreens WHERE ScreenCode = 'EMPPROF');

    IF COLUMNPROPERTY(OBJECT_ID('dbo.PayrollScreens'), 'IdPayrollScreen', 'IsIdentity') = 1
        INSERT INTO PayrollScreens (ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled)
        VALUES ('View Employee Documents', 'EMPDOCVIEW', 'V', @Parent, ISNULL(@Ord, 0) + 1, 'PAYROLL', 1);
    ELSE
        INSERT INTO PayrollScreens (IdPayrollScreen, ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled)
        SELECT MAX(IdPayrollScreen) + 1, 'View Employee Documents', 'EMPDOCVIEW', 'V', @Parent, ISNULL(@Ord, 0) + 1, 'PAYROLL', 1
        FROM PayrollScreens;
END

SELECT * FROM PayrollScreens WHERE ScreenCode IN ('EMPPROF', 'EMPDOCVIEW');
