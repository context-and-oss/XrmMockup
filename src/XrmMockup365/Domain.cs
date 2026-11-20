namespace DG.Tools.XrmMockup {

    /// <summary>
    /// Enum for defining the scope of the registered plugin
    /// </summary>
    public enum PluginRegistrationScope {
        Permanent = 0,
        Temporary = 1,
    }

    internal enum FullNameConventionCode {
        LastFirst = 0,
        FirstLast = 1,
        LastFirstMiddleInitial = 2,
        FirstMiddleInitialLast = 3,
        LastFirstMiddle = 4,
        FirstMiddleLast = 5,
        LastSpaceFirst = 6,
        LastNoSpaceFirst = 7
    }    

    internal enum SourceType
    {
        SimpleAttribute = 0,
        CalculatedAttribute = 1,
        RollupAttribute = 2,
        FormulaAttribute = 3
    }
}
