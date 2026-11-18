# Classic numeric calculated fields

These are Dataverse-exported `FormulaDefinition` values (`SourceType = 1`), formatted as UTF-8 XML:

- `MoneyToWholeNumber.xml`: `account.ns_simplecalculatedfieldtest = ns_moneyfield`.
- `MoneyArithmeticToWholeNumber.xml`: `account.ns_calculatedfieldtest = (ns_moneyfield / 2) * 2`.

Both output columns are Whole Number (`IntegerAttributeMetadata`), and the source is Money.
Dataverse rounds the final result to the nearest integer, with midpoint rounding away from zero.
The direct mapping preserves empty values. Despite the arithmetic operators being tagged `Int32`
in the XAML, they must retain intermediate fractions: a Money value of `1.01` produces `1`, not `2`.

## Money output fields

The additional Dataverse exports reverse the conversion:

- `NumberToMoney.xml`: `account.ns_simplecalculatedmoneyfield = ns_simplecalculatedfieldtest`.
- `NumberArithmeticToMoney.xml`: `account.ns_calculatedmoneyfield = (ns_simplecalculatedfieldtest / 2) * 2`.

The source is the previously calculated whole-number field; the outputs are Money. The direct
mapping has no arithmetic node, so final assignment must wrap its integer value in `Money`.
`TestMoneyCalculatedFields` also exercises decimal sources by changing only the source attribute
name, preserving all operators and target types from the exports.

Both test classes parse and execute these exports without persistence. These unit tests do not
require the `ns_*` columns to be added to the shared account metadata or the live test environment.
