# Infor Visual CSV Inventory

## Glossary

| Term | Meaning |
| --- | --- |
| `CSV` | Comma-separated values, a plain-text table format used for exports. |
| `MTMFG` | The SQL Server database being researched for the Infor VISUAL environment. |
| `Schema` | The structure of a database: tables, columns, keys, constraints, and related metadata. |
| `FK` | Foreign key, a declared relationship from one table to another. |
| `PK` | Primary key, the column or column set that uniquely identifies a row. |
| `Constraint` | A database rule, such as a default, unique, or check rule. |
| `Trigger` | Database logic that runs automatically when certain table changes happen. |
| `Read-only permissions` | Access that allows viewing data and metadata but not changing database objects or rows. |

| File | Purpose |
| --- | --- |
| `MTMFG_Schema_Tables.csv` | Table and column inventory across base tables. |
| `MTMFG_Schema_FKs.csv` | Declared foreign key paths between tables. |
| `MTMFG_Schema_PKs.csv` | Primary key composition by table. |
| `MTMFG_Schema_ColumnDetails.csv` | Column length, precision, nullability, identity, and computed flags. |
| `MTMFG_Schema_Views.csv` | View column inventory for SQL Server views. |
| `MTMFG_Schema_Indexes.csv` | Index coverage, uniqueness, and included-column details. |
| `MTMFG_Schema_TableRowCounts.csv` | Table size and scale awareness. |
| `MTMFG_Schema_UniqueConstraints.csv` | Unique key and business-key discovery. |
| `MTMFG_Schema_Triggers.csv` | Trigger coverage and table event visibility. |
| `MTMFG_Schema_DefaultConstraints.csv` | Default-constraint presence inventory under read-only permissions. |
| `MTMFG_Schema_CheckConstraints.csv` | Check-constraint presence inventory under read-only permissions. |
| `MTMFG_Research_StatusValueProfiles.csv` | Live status code values used in customer-order and work-order tables. |
| `MTMFG_Research_CustOrderAlloc_TypeProfiles.csv` | Demand/supply type semantics inside `CUST_ORDER_ALLOC`. |
| `MTMFG_Research_CustOrderAlloc_Samples.csv` | Representative bridge-row samples from `CUST_ORDER_ALLOC`. |
| `MTMFG_Research_WorkOrderCustomerOrderCoverage.csv` | Verified customer-order to work-order bridge coverage via structured `CUST_ORDER_ALLOC` keys. |
| `MTMFG_Research_TargetColumnInventory.csv` | Focused inventory of relationship, status, and chronology columns used in current research. |
| `MTMFG_Research_BridgeColumnPopulation.csv` | Population coverage of candidate bridge columns before trusting them for joins. |
| `MTMFG_Research_DateFieldRanges.csv` | Population and min/max ranges for the date fields most relevant to order chronology. |
| `MTMFG_Research_WorkOrderOperationStatusMatrix.csv` | Frequency matrix of `W`-type work-order and operation status pairs. |
| `MTMFG_Research_WorkOrderOperationStatusSamples.csv` | Row-level samples for each `W`-type work-order and operation status pair. |
| `MTMFG_Research_ShopResourceWorkcenterProfiles.csv` | Workcenter meaning and usage profile from `SHOP_RESOURCE` plus `W`-type operations. |
| `MTMFG_Research_WorkOrderMaterialSiteCoverage.csv` | Coverage of `MTM2` part-site matches for work-order requirement parts. |
| `MTMFG_Research_WorkOrderMaterialSamples_SingleSequence.csv` | Material samples for `W` work orders that have exactly one distinct operation sequence. |
| `MTMFG_Research_WorkOrderMaterialSamples_MultiSequence.csv` | Material samples for `W` work orders that have more than one distinct operation sequence. |
