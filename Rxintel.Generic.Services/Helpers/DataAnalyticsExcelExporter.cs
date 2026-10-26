using ClosedXML.Excel;
using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.Generic.Services.Helpers
{
    public class DataAnalyticsExcelExporter
    {
        private enum ColumnType
        {
            Text,
            Number,
            Currency,
            NetBadge
        }

        private class ColumnDef
        {
            public string Key { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public ColumnType Type { get; set; } = ColumnType.Text;
            public Func<DataAnalyticsRowDto, object?> ValueSelector { get; set; } = _ => "";
            public XLAlignmentHorizontalValues Alignment { get; set; } = XLAlignmentHorizontalValues.Center;
        }

        private class ColumnGroupDef
        {
            public string Label { get; set; } = "";
            public XLColor GroupColor { get; set; } = XLColor.LightGray;
            public List<ColumnDef> Columns { get; set; } = new();
        }

        public byte[] Export(DataAnalyticsResponseDto analytics) =>
            Export(analytics, null, null);

        public byte[] Export(
            DataAnalyticsResponseDto analytics,
            IEnumerable<string>? selectedColumns,
            IEnumerable<string>? hiddenColumns = null,
            bool advancedView = false)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Report");
            BuildAnalyticsWorksheet(worksheet, analytics, selectedColumns, hiddenColumns, advancedView);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void BuildAnalyticsWorksheet(
            IXLWorksheet worksheet,
            DataAnalyticsResponseDto analytics,
            IEnumerable<string>? selectedColumns,
            IEnumerable<string>? hiddenColumns,
            bool advancedView)
        {
            var selectedSet = selectedColumns?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var hiddenSet = hiddenColumns?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            bool IsColumnIncluded(string name, params string[] aliases)
            {
                if (selectedSet != null && selectedSet.Count > 0)
                {
                    if (selectedSet.Contains(name)) return true;
                    return aliases.Any(a => selectedSet.Contains(a));
                }

                if (hiddenSet != null && hiddenSet.Count > 0)
                {
                    if (hiddenSet.Contains(name)) return false;
                    return !aliases.Any(a => hiddenSet.Contains(a));
                }

                return true;
            }

            var allFixedColumns = new List<(ColumnDef Def, string[] Aliases)>
            {
                (new ColumnDef { Key = "NDC", DisplayName = "NDC", Type = ColumnType.Text, Alignment = XLAlignmentHorizontalValues.Left, ValueSelector = r => r.Ndc }, Array.Empty<string>()),
                (new ColumnDef { Key = "Product Name", DisplayName = "Product Name", Type = ColumnType.Text, Alignment = XLAlignmentHorizontalValues.Left, ValueSelector = r => r.ProductName }, Array.Empty<string>()),
                (new ColumnDef { Key = "Package Size", DisplayName = "Package Size", Type = ColumnType.Text, Alignment = XLAlignmentHorizontalValues.Center, ValueSelector = r => r.PackageSize ?? "" }, Array.Empty<string>()),
                (new ColumnDef { Key = "Verified", DisplayName = "Verified", Type = ColumnType.Text, Alignment = XLAlignmentHorizontalValues.Center, ValueSelector = r => r.Verified }, Array.Empty<string>()),
                (new ColumnDef { Key = "Wholesaler", DisplayName = "Wholesaler", Type = ColumnType.Text, Alignment = XLAlignmentHorizontalValues.Center, ValueSelector = r => r.Source }, new[] { "Source" }),
                (new ColumnDef { Key = "Quantity Ordered", DisplayName = "Quantity Ordered", Type = ColumnType.Number, Alignment = XLAlignmentHorizontalValues.Right, ValueSelector = r => r.TotalQuantityOrdered }, new[] { "Total Quantity Ordered" }),
                (new ColumnDef { Key = "Billed Quantity", DisplayName = "Billed Quantity", Type = ColumnType.Number, Alignment = XLAlignmentHorizontalValues.Right, ValueSelector = r => r.TotalBilledQuantity }, new[] { "Total Billed Quantity" }),
                (new ColumnDef { Key = "Net Quantity", DisplayName = "Net Quantity", Type = ColumnType.NetBadge, Alignment = XLAlignmentHorizontalValues.Right, ValueSelector = r => r.NetQuantity }, Array.Empty<string>()),
                (new ColumnDef { Key = "Dollars Billed", DisplayName = "Dollars Billed", Type = ColumnType.Currency, Alignment = XLAlignmentHorizontalValues.Right, ValueSelector = r => r.DollarsBilledTotal }, new[] { "Dollars Billed (Total)", "Dollars Billed Total" })
            };

            var activeFixedColumns = allFixedColumns
                .Where(x => IsColumnIncluded(x.Def.Key, x.Aliases))
                .Select(x => x.Def)
                .ToList();

            var primaryNames = analytics.PrimaryInsuranceColumnNames ?? new List<string>();
            var wholesalerNames = analytics.WholesalerColumnNames ?? new List<string>();
            var families = analytics.InsuranceFamilies ?? new List<InsuranceFamilyDto>();
            var familyMemberNames = families
                .SelectMany(f => f.MemberNames)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var wholesalerQuantityGroup = new ColumnGroupDef
            {
                Label = "Wholesaler Quantity",
                GroupColor = XLColor.FromHtml("#BDD7EE"),
                Columns = wholesalerNames
                    .Select(name => new ColumnDef
                    {
                        Key = $"{name} Qty",
                        DisplayName = name,
                        Type = ColumnType.Number,
                        Alignment = XLAlignmentHorizontalValues.Right,
                        ValueSelector = r => r.WholesalerQuantities.GetValueOrDefault(name)
                    })
                    .Where(c => IsColumnIncluded(c.Key, [c.DisplayName]))
                    .ToList()
            };

            List<ColumnGroupDef> packageAndNetGroups;
            if (advancedView && families.Count > 0)
            {
                var familyPackageGroups = families.Select(family => new ColumnGroupDef
                {
                    Label = family.ParentName,
                    GroupColor = XLColor.FromHtml("#C5D4F5"),
                    Columns = BuildFamilyPackageColumns(family, (name, aliases) => IsColumnIncluded(name, aliases))
                }).Where(g => g.Columns.Count > 0).ToList();

                var standaloneNames = primaryNames
                    .Where(name => !familyMemberNames.Contains(name))
                    .ToList();

                if (standaloneNames.Count > 0)
                {
                    familyPackageGroups.Add(new ColumnGroupDef
                    {
                        Label = "Quality billed",
                        GroupColor = XLColor.FromHtml("#FFD966"),
                        Columns = standaloneNames
                            .Select(name => new ColumnDef
                            {
                                Key = $"{name} Packages",
                                DisplayName = name,
                                Type = ColumnType.Number,
                                Alignment = XLAlignmentHorizontalValues.Right,
                                ValueSelector = r => r.ActualInsurancePackages.GetValueOrDefault(name)
                            })
                            .Where(c => IsColumnIncluded(c.Key))
                            .ToList()
                    });
                }

                packageAndNetGroups =
                [
                    .. familyPackageGroups,
                    new ColumnGroupDef
                    {
                        Label = "Excess qualities",
                        GroupColor = XLColor.FromHtml("#F4B084"),
                        Columns = primaryNames
                            .Select(name => new ColumnDef
                            {
                                Key = $"{name} Net",
                                DisplayName = name,
                                Type = ColumnType.NetBadge,
                                Alignment = XLAlignmentHorizontalValues.Right,
                                ValueSelector = r => r.PrimaryInsuranceNet.GetValueOrDefault(name)
                            })
                            .Where(c => IsColumnIncluded(c.Key))
                            .ToList()
                    }
                ];
            }
            else
            {
                packageAndNetGroups =
                [
                    new ColumnGroupDef
                    {
                        Label = "Quality billed",
                        GroupColor = XLColor.FromHtml("#FFD966"),
                        Columns = primaryNames
                            .Select(name =>
                            {
                                var key = $"{name} Packages";
                                return new ColumnDef
                                {
                                    Key = key,
                                    DisplayName = name,
                                    Type = ColumnType.Number,
                                    Alignment = XLAlignmentHorizontalValues.Right,
                                    ValueSelector = r => r.PrimaryInsurancePackages.GetValueOrDefault(name)
                                };
                            })
                            .Where(c => IsColumnIncluded(c.Key, [$"{c.Key} (Primary)", c.Key.Replace(" Packages", " Packages (Primary)")]))
                            .ToList()
                    },
                    new ColumnGroupDef
                    {
                        Label = "Excess qualities",
                        GroupColor = XLColor.FromHtml("#F4B084"),
                        Columns = primaryNames
                            .Select(name => new ColumnDef
                            {
                                Key = $"{name} Net",
                                DisplayName = name,
                                Type = ColumnType.NetBadge,
                                Alignment = XLAlignmentHorizontalValues.Right,
                                ValueSelector = r => r.PrimaryInsuranceNet.GetValueOrDefault(name)
                            })
                            .Where(c => IsColumnIncluded(c.Key))
                            .ToList()
                    }
                ];
            }

            var dollarsBilledGroup = new ColumnGroupDef
            {
                Label = "Dollars Billed",
                GroupColor = XLColor.FromHtml("#FFE699"),
                Columns = primaryNames
                    .Select(name => new ColumnDef
                    {
                        Key = $"{name} Dollars",
                        DisplayName = name,
                        Type = ColumnType.Currency,
                        Alignment = XLAlignmentHorizontalValues.Right,
                        ValueSelector = r => r.PrimaryInsuranceDollars.GetValueOrDefault(name)
                    })
                    .Where(c => IsColumnIncluded(c.Key))
                    .ToList()
            };

            var activeGroups = new List<ColumnGroupDef>();
            if (wholesalerQuantityGroup.Columns.Count > 0)
                activeGroups.Add(wholesalerQuantityGroup);
            activeGroups.AddRange(packageAndNetGroups);
            activeGroups.Add(dollarsBilledGroup);
            activeGroups = activeGroups.Where(g => g.Columns.Count > 0).ToList();

            if (activeFixedColumns.Count == 0 && activeGroups.Count == 0)
            {
                activeFixedColumns = allFixedColumns.Select(x => x.Def).ToList();
            }

            var hasGroups = activeGroups.Count > 0;
            const int headerRow = 1;
            var columnHeaderRow = hasGroups ? 2 : 1;
            var dataStartRow = hasGroups ? 3 : 2;

            var allActiveColumns = new List<ColumnDef>();
            var colIndex = 1;

            foreach (var colDef in activeFixedColumns)
            {
                allActiveColumns.Add(colDef);
                var cell = worksheet.Cell(headerRow, colIndex);
                cell.Value = colDef.DisplayName;

                if (hasGroups)
                {
                    worksheet.Range(headerRow, colIndex, columnHeaderRow, colIndex).Merge();
                }

                colIndex++;
            }

            foreach (var group in activeGroups)
            {
                var groupStartCol = colIndex;
                var groupEndCol = colIndex + group.Columns.Count - 1;

                var groupCell = worksheet.Range(headerRow, groupStartCol, headerRow, groupEndCol).Merge().FirstCell();
                groupCell.Value = group.Label;
                ApplyGroupHeaderStyle(groupCell, group.GroupColor);

                foreach (var colDef in group.Columns)
                {
                    allActiveColumns.Add(colDef);
                    worksheet.Cell(columnHeaderRow, colIndex).Value = colDef.DisplayName;
                    colIndex++;
                }
            }

            var totalColumns = allActiveColumns.Count;

            if (activeFixedColumns.Count > 0)
            {
                ApplyColumnHeaderStyle(worksheet.Range(headerRow, 1, columnHeaderRow, activeFixedColumns.Count));
            }
            if (totalColumns > activeFixedColumns.Count)
            {
                ApplyColumnHeaderStyle(worksheet.Range(columnHeaderRow, activeFixedColumns.Count + 1, columnHeaderRow, totalColumns));
            }

            var currentRow = dataStartRow;
            foreach (var rowData in analytics.Data)
            {
                for (var i = 0; i < allActiveColumns.Count; i++)
                {
                    var colDef = allActiveColumns[i];
                    var cell = worksheet.Cell(currentRow, i + 1);
                    var val = colDef.ValueSelector(rowData);

                    switch (colDef.Type)
                    {
                        case ColumnType.Currency:
                            var currVal = Convert.ToDecimal(val ?? 0);
                            cell.Value = (double)currVal;
                            cell.Style.NumberFormat.Format = "$#,##0.00";
                            break;

                        case ColumnType.Number:
                            var numVal = Convert.ToDecimal(val ?? 0);
                            cell.Value = (double)numVal;
                            cell.Style.NumberFormat.Format = "#,##0.##";
                            break;

                        case ColumnType.NetBadge:
                            var netVal = Convert.ToDecimal(val ?? 0);
                            cell.Value = (double)netVal;
                            cell.Style.NumberFormat.Format = "#,##0.##";
                            cell.Style.Fill.BackgroundColor = netVal < 0
                                ? XLColor.FromHtml("#FFC7CE")
                                : XLColor.FromHtml("#C6EFCE");
                            break;

                        default:
                            cell.Value = val?.ToString() ?? "";
                            break;
                    }

                    cell.Style.Alignment.Horizontal = colDef.Alignment;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                currentRow++;
            }

            var lastDataRow = Math.Max(dataStartRow, currentRow - 1);
            if (totalColumns > 0)
            {
                var usedRange = worksheet.Range(headerRow, 1, lastDataRow, totalColumns);
                usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                worksheet.Columns(1, totalColumns).AdjustToContents();

                for (var i = 0; i < allActiveColumns.Count; i++)
                {
                    var colDef = allActiveColumns[i];
                    var colNum = i + 1;
                    if (colDef.Key.Equals("Product Name", StringComparison.OrdinalIgnoreCase))
                    {
                        worksheet.Column(colNum).Width = Math.Max(worksheet.Column(colNum).Width, 36);
                    }
                    else
                    {
                        worksheet.Column(colNum).Width = Math.Max(worksheet.Column(colNum).Width, 12);
                    }
                }

                worksheet.SheetView.FreezeRows(hasGroups ? 2 : 1);

                if (lastDataRow >= columnHeaderRow)
                {
                    worksheet.Range(columnHeaderRow, 1, lastDataRow, totalColumns).SetAutoFilter();
                }
            }
        }

        private static List<ColumnDef> BuildFamilyPackageColumns(
            InsuranceFamilyDto family,
            Func<string, string[], bool> isColumnIncluded)
        {
            return family.MemberNames
                .Select(name => new ColumnDef
                {
                    Key = $"{name} Packages",
                    DisplayName = name,
                    Type = ColumnType.Number,
                    Alignment = XLAlignmentHorizontalValues.Right,
                    ValueSelector = r => r.ActualInsurancePackages.GetValueOrDefault(name)
                })
                .Where(c => isColumnIncluded(c.Key, []))
                .ToList();
        }

        private static List<ColumnDef> BuildFamilyNetColumns(
            InsuranceFamilyDto family,
            Func<string, string[], bool> isColumnIncluded)
        {
            return family.MemberNames
                .Select(name => new ColumnDef
                {
                    Key = $"{name} Net",
                    DisplayName = name,
                    Type = ColumnType.NetBadge,
                    Alignment = XLAlignmentHorizontalValues.Right,
                    ValueSelector = r => r.ActualInsuranceNet.GetValueOrDefault(name)
                })
                .Where(c => isColumnIncluded(c.Key, []))
                .ToList();
        }

        private static void ApplyColumnHeaderStyle(IXLRange range)
        {
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Font.Bold = true;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.WrapText = true;
        }

        private static void ApplyGroupHeaderStyle(IXLCell cell, XLColor backgroundColor)
        {
            cell.Style.Fill.BackgroundColor = backgroundColor;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }
    }
}
