using DG.Tools.XrmMockup.CustomFunction;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Dataverse;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PowerFxDataverseConnection = Microsoft.PowerFx.Dataverse.DataverseConnection;

namespace DG.Tools.XrmMockup
{
    internal class FormulaFieldEvaluator
    {
        private readonly IOrganizationService _organizationService;
        private readonly PowerFxDataverseConnection _dataverseConnection;

        public FormulaFieldEvaluator(IOrganizationServiceFactory serviceFactory)
        {
            _organizationService = serviceFactory.CreateOrganizationService(null);
            _dataverseConnection = SingleOrgPolicy.New(_organizationService);
        }

        public async Task<object> Evaluate(string formula, Entity thisEntity, TimeSpan timeOffset)
        {
            var rowScopeSymbols = _dataverseConnection.GetRowScopeSymbols(thisEntity.LogicalName, true);

            var config = new PowerFxConfig();
            config.AddFunction(new UTCTodayFunction(timeOffset));
            config.AddFunction(new UTCNowFunction(timeOffset));
            config.AddFunction(new IsUTCTodayFunction(timeOffset));
            config.AddFunction(new ISOWeekNumFunction(timeOffset));

            var engine = new RecalcEngine(config);

            var combinedSymbols = ReadOnlySymbolTable.Compose(rowScopeSymbols, _dataverseConnection.Symbols);
            var checkResult = engine.Check(formula, new ParserOptions(CultureInfo.InvariantCulture), combinedSymbols);
            checkResult.ThrowOnErrors();

            var thisRecord = _dataverseConnection.Marshal(thisEntity);
            var rowScopeValues = ReadOnlySymbolValues.Compose(
                ReadOnlySymbolValues.NewFromRecord(rowScopeSymbols, thisRecord),
                _dataverseConnection.SymbolValues
            );

            var computedValue = await checkResult.GetEvaluator().EvalAsync(CancellationToken.None, new RuntimeConfig(rowScopeValues));
            return computedValue.ToObject();
        }
    }
}
