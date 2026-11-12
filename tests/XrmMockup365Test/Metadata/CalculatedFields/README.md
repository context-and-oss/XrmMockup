# Classic whole-number calculated fields

These are Dataverse-exported `FormulaDefinition` values (`SourceType = 1`), formatted as UTF-8 XML:

- `MoneyToWholeNumber.xml`: `account.ns_simplecalculatedfieldtest = ns_moneyfield`.
- `MoneyArithmeticToWholeNumber.xml`: `account.ns_calculatedfieldtest = (ns_moneyfield / 2) * 2`.

Both output columns are Whole Number (`IntegerAttributeMetadata`), and the source is Money.
Dataverse rounds the final result to the nearest integer, with midpoint rounding away from zero.
The direct mapping preserves empty values. Despite the arithmetic operators being tagged `Int32`
in the XAML, they must retain intermediate fractions: a Money value of `1.01` produces `1`, not `2`.

`TestWholeNumberCalculatedFields` parses and executes these exports without persistence. These
unit tests do not require the `ns_*` columns to be added to the shared account metadata or the
live test environment.
