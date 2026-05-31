# VISUAL Inbound BOD Working Reference

## Glossary

| Term | Meaning |
| --- | --- |
| `BOD` | Business Object Document, an Infor integration message format. |
| `VISUAL` | Infor VISUAL, the ERP and manufacturing system used in the source mappings. |
| `ERP` | Enterprise Resource Planning, the business system that manages orders, inventory, and manufacturing records. |
| `Inbound` | Data coming into VISUAL from another system. |
| `Outbound` | Data leaving VISUAL for another system. |
| `MRO` | Maintenance, repair, and operations inventory or parts. |
| `UOM` | Unit of measure, such as each, feet, or pounds. |

This file is a curated working extract of the original Infor VISUAL inbound BOD mapping guide.

It intentionally removes:

- legal boilerplate and publication filler
- page headers, page numbers, and OCR noise
- repetitive UI wording that does not help implementation
- low-value address/contact permutations that can be recovered later from the original vendor document if needed

It keeps what is useful for this repository now and what is likely to matter later when the integration surface expands.

## Scope

The current app and research work are centered on these VISUAL domains:

- work orders, operations, requirements, resources, and statuses
- customer orders and ship schedules
- item and item-site master data
- site, warehouse, and ship-to identifiers
- likely-next procurement and receiving flows

## Priority Map

| Priority | BOD | Why it matters |
| --- | --- | --- |
| Current | `ProductionOrder` | Best inbound work-order reference for `WORK_ORDER`, `OPERATION`, `REQUIREMENT`, `CO_PRODUCT`, and resource assignment behavior. |
| Current | `SalesOrder` | Best inbound customer-order reference for `CUSTOMER_ORDER`, `CUST_ORDER_LINE`, and delivery schedule behavior. |
| Current | `ItemMaster` | Best inbound part and part-site reference for `PART`, `PART_SITE`, planning, status, and classification fields. |
| Current | `Location` | Explains how site, warehouse, office, and ship-to IDs are composed in BOD payloads. |
| Near-term | `CustomerPartyMaster` | Useful for customer master, site assignment, terms, sales rep, and customer metadata sync. |
| Near-term | `ShipToPartyMaster` | Useful for ship-to address and ship-to identifier behavior. |
| Near-term | `PurchaseOrder` | Relevant if we later bridge supply-side demand assignment or inbound purchasing flows. |
| Near-term | `ReceiveDelivery` | Relevant for receipt-side inventory and inbound material events. |
| Near-term | `InventoryAdjustment` | Useful if inventory movement sync becomes part of the integration. |
| Near-term | `InspectionOrder` | Useful if receiving inspection becomes part of the integration surface. |
| Reference only | `BillOfResources` | Similar structure to work-order detail, but explicitly aimed at engineering masters rather than production work orders. |
| Reference only | `Requisition`, `Quote` | Not current priorities, but still plausible future reference material. |

## Core Notes

- `BillOfResources` is for engineering masters. The source explicitly says it is generated for type `M`, not normal production work orders.
- `ProductionOrder` is the better inbound reference for real work-order behavior.
- Many BOD codes rely on VISUAL code translation rules through Code Mapping Maintenance. Keep that in mind when comparing raw database values to payload values.
- Many status fields in BODs are translated values, not raw database values.
- Several BOD IDs are composed keys, often using `~` as a delimiter.

## Shared Status Translation Patterns

### Work-order style status mapping

Used repeatedly in `ProductionOrder`, and similarly in `BillOfResources` sections for header, operations, and consumed items.

| VISUAL meaning | BOD code |
| --- | --- |
| Planned | `U` |
| Firm | `F` |
| Active or Released | `R` |
| Canceled | `X` |
| Closed | `C` |

### Sales-order status mapping

| BOD status | VISUAL result |
| --- | --- |
| Open | `F` |
| Approved | `R` |
| Closed | `C` |
| Shipped | `C` |
| Invoiced | `C` |
| Pending | `H` |
| Hold | `H` |
| Working | `H` |
| Canceled | `X` |
| Deleted | `X` |
| Unapproved | `F` |

### Purchase-order status mapping

| VISUAL status | BOD code |
| --- | --- |
| Ordered | `R` |
| Open | `R` |
| Unapproved | `F` |
| Planned | `F` |
| Closed | `C` |
| Canceled | `X` |

## ProductionOrder

### Why keep it

This is the most useful inbound reference for the tables we have been researching directly:

- `WORK_ORDER`
- `OPERATION`
- `REQUIREMENT`
- `CO_PRODUCT`
- `OPERATION_RESOURCE`
- resource assignment behavior tied to work centers and concurrent resources

### Header fields worth keeping

| BOD path | VISUAL source | Why it matters |
| --- | --- | --- |
| `ProductionOrderHeader/DocumentID/ID` | `work_order.type ~ base_id ~ lot_id ~ split_id ~ 0` | Confirms the composed production-order identity shape. |
| `ProductionOrderHeader/DocumentDateTime` | `work_order.create_date` | Useful for chronology and source timestamps. |
| `ProductionOrderHeader/Status/Code` | `work_order.status` | Confirms translated work-order status behavior. |
| `ProductionOrderHeader/Status/EffectiveDateTime` | `work_order.status_eff_date` plus transaction logic | Important for interpreting lifecycle timing. |
| `ProductionOrderHeader/OrderQuantity` | `work_order.desired_qty` | Confirms order quantity source. |
| `ProductionOrderHeader/ForecastedTimePeriod/StartDateTime` | `work_order.sched_start_date` | Scheduled start reference. |
| `ProductionOrderHeader/ForecastedTimePeriod/EndDateTime` | `work_order.sched_finish_date` | Scheduled finish reference. |
| `ProductionOrderHeader/DueDateTime` | `work_order.desired_want_date` | Due-date reference. |
| `ProductionOrderHeader/UserArea/Property/NameValue` | `work_order.user_1..user_10`, `udf_layout_id`, `site_id`, `global_rank` | Preserves custom fields and site/rank metadata. |
| `ProductionOrderHeader/ForwardScheduleIndicator` | `work_order.forward_schedule` | Important if scheduling behavior matters later. |
| `ProductionOrderHeader/Costing/Amount` | `work_order.est_*` planned costs | Useful only if planned cost sync is in scope. |
| `ProductionOrderHeader/EarliestStartDateTime` | `work_order.desired_rls_date`, `hard_release_date` | Important for release-date semantics. |

### Operation-level mappings worth keeping

| BOD path | VISUAL source | Why it matters |
| --- | --- | --- |
| `ProductionOrderDetail/BillOfResources/Operations/ID` | composed from `operation.workorder_*` plus `sequence_no` | Confirms operation identity shape. |
| `.../Operations/NextID` | composed key using next operation or next sub-item context | Useful when sequence chaining matters. |
| `.../Operations/Status/Code` | `operation.status` | Core operation status reference. |
| `.../Operations/Status/EffectiveDateTime` | `operation.status_eff_date` plus transaction logic | Important for status chronology. |
| `.../Operations/ConstrainedResourceReference/ResourceID/ID` | `operation.resource_id`, `operation_resource.resource_id`, `operation.max_downtime` | Most useful inbound reference for workcenter and concurrent-resource semantics. |
| `.../Operations/ProcessCode` | `operation.operation_type` | Operation type reference. |
| `.../Operations/TransferLotQuantity` | `operation.minimum_move_qty` | Useful for movement logic. |
| `.../Operations/SetupTimeDuration` | `operation.setup_hrs` | Setup duration mapping. |
| `.../Operations/WaitTimeDuration` | `operation.max_gap_prev_op` | Gap/wait semantics. |
| `.../Operations/RunTimeDuration` | `operation.run_type`, `operation.run`, `operation.load_size_qty` | Core runtime semantics. |
| `.../Operations/BatchDuration` | `operation.transit_days`, `run_hrs`, `run` | Batch-duration fallback behavior. |
| `.../Operations/MoveDuration` | `operation.move_hrs` | Move-hour mapping. |
| `.../Operations/RejectPercent` | `operation.scrap_yield_pct` | Scrap-vs-yield interpretation. |
| `.../Operations/YieldPercent` | `operation.scrap_yield_pct` | Same source, opposite interpretation. |
| `.../Operations/UserArea/Property/NameValue` | `operation.user_*`, cost/burden/run fields | Useful for later deep cost/run tuning, not for day-one integration. |

### Material and requirement mappings worth keeping

| BOD path | VISUAL source | Why it matters |
| --- | --- | --- |
| `.../ConsumedItem/ItemID/ID` | `requirement.part_id` | Core material-component link. |
| `.../ConsumedItem/DocumentReference/DocumentID/ID` | composed from work-order key, sequence, and `requirement.piece_no` | Confirms how piece-level requirement identity is formed. |
| `.../ConsumedItem/Status/Code` | requirement status translation | Important if requirement lifecycle gets surfaced later. |
| `.../ConsumedItem/Quantity` | `requirement.calc_qty`, `requirement.usage_um` | Core quantity and U/M mapping. |
| `.../ConsumedItem/Costing/Amount` | `requirement.est_*`, `unit_*`, `fixed_cost`, `burden_per_unit` | Useful only if component costing becomes relevant later. |
| `.../ConsumedItem/ScrapFactor` | `requirement.scrap_percent` | Material scrap mapping. |
| `.../ConsumedItem/ScrapQuantity` | `requirement.calc_fixed_scrap` | Fixed scrap amount. |
| `.../ConsumedItem/EffectiveTimePeriod/*` | `requirement.effective_date`, `discontinue_date` | Effective/discontinue timing. |
| `.../ConsumedItem/AlternateVersion/AlternateDocumentID/ID` | `req_part_alternate.part_id`, `work_order.allow_alt_parts` | Alternate-part behavior. |
| `.../ConsumedItem/LeadTimeDuration` | `requirement.planning_leadtime` | Material planning lead time. |
| `.../ConsumedItem/UserArea/Property/NameValue` | `requirement.user_*`, `qty_per`, `fixed_qty`, `qty_per_type`, subordinate WO link, `burden_percent` | Useful for later subordinate-order and advanced quantity behavior. |

### Output item behavior worth keeping

| BOD path | VISUAL source | Why it matters |
| --- | --- | --- |
| `.../OutputItem/ItemID/ID` | first item -> `work_order.part_id`, later items -> `co_product.part_id` | Confirms co-product behavior. |
| `.../OutputItem/Quantity` | first item -> `work_order.desired_qty`, later items -> `co_product.desired_qty` | Important if co-products ever matter. |
| `.../OutputItem/UserArea/Property/NameValue` | `work_order.variable_table` | Only relevant if variable-table calculation behavior is needed later. |

## BillOfResources

### Keep only the part that matters

`BillOfResources` mirrors much of the operation and requirement structure above, but the source explicitly states it is for engineering masters and type `M`.

That makes it lower priority than `ProductionOrder` for this repo.

### What is still worth remembering

- header identity is composed from `work_order.type`, `base_id`, `lot_id`, `split_id`, and `sub_id`
- operation identity is composed from work-order identity plus `operation.sequence_no`
- consumed-item identity adds `requirement.piece_no`
- the same translated status family appears here
- `ConstrainedResourceReference`, `ProcessCode`, setup/run/move duration, and scrap/yield semantics broadly mirror `ProductionOrder`

If we later need engineering-master import behavior specifically, restore the older raw document from git history and compare it against `ProductionOrder`.

## SalesOrder

### Why keep it

This is the best inbound reference for:

- `CUSTOMER_ORDER`
- `CUST_ORDER_LINE`
- `CUST_LINE_DEL`
- `CUST_ADDRESS`

### Header fields worth keeping

| BOD path | VISUAL source | Why it matters |
| --- | --- | --- |
| `SalesOrderHeader/DocumentID/ID` | `customer_order.id` | Primary customer-order identity. |
| `SalesOrderHeader/DocumentID/@location` | `Site ~ customer_order.site_id` | Important for site-qualified sales orders. |
| `SalesOrderHeader/DocumentDateTime` | `customer_order.order_date` | Order chronology. |
| `SalesOrderHeader/Status/Code` | translated from `customer_order.status` and site state | Core sales-order status behavior. |
| `SalesOrderHeader/CustomerParty/PartyIDs/ID` | `customer_order.customer_id` | Customer identity link. |
| `SalesOrderHeader/ShipToParty/PartyIDs/ID` | `customer_order.customer_id ~ cust_address.shipto_id` | Ship-to identity composition. |
| `SalesOrderHeader/RequestedShipDateTime` | `customer_order.desired_ship_date` | Requested ship date. |
| `SalesOrderHeader/PromisedShipDateTime` | `customer_order.promise_date` | Promise ship date. |
| `SalesOrderHeader/PromisedDeliveryDateTime` | `customer_order.promise_del_date` | Promise delivery date. |
| `SalesOrderHeader/PurchaseOrderReference/DocumentID/ID` | `customer_order.customer_po_ref` | Customer PO link. |
| `SalesOrderHeader/PaymentTerm/Term/ID` | customer-order terms fields | Terms behavior. |
| `SalesOrderHeader/SalesPersonReference/IDs/ID` | `customer_order.salesrep_id` | Sales rep mapping. |
| `SalesOrderHeader/UserArea/Property/NameValue` | `customer_order.user_*`, ship via, contact details, UDF layout | Important because free-form contact details may live here. |

### Line and schedule fields worth keeping

| BOD path | VISUAL source | Why it matters |
| --- | --- | --- |
| `SalesOrderLine/LineNumber` | `cust_order_line.line_no` | Line identity. |
| `SalesOrderLine/Status/Code` | `cust_order_line.line_status` | Source notes say inbound updates the line to `A`. |
| `SalesOrderLine/Status/EffectiveDateTime` | line, order, shipper, and receivable timing logic | Useful for lifecycle interpretation. |
| `SalesOrderLine/Item/ItemID/ID` | `cust_order_line.part_id` or `service_charge_id` | Part vs service behavior. |
| `SalesOrderLine/Quantity` | `cust_order_line.user_order_qty` | Ordered quantity. |
| `SalesOrderLine/BaseUOMQuantity` | `cust_order_line.order_qty` | Stock-order quantity. |
| `SalesOrderLine/UnitPrice/Amount` | `cust_order_line.unit_price` | Price mapping. |
| `SalesOrderLine/RequiredDeliveryDateTime` | `cust_order_line.desired_ship_date` | Required ship date at line level. |
| `SalesOrderLine/ShipFromParty/Location/ID` | `Warehouse ~ cust_order_line.warehouse_id` | Warehouse identity behavior. |
| `SalesOrderLine/AllocatedBaseUOMQuantity` | `cust_order_line.allocated_qty` | Important for allocation state. |
| `SalesOrderLine/ShippedQuantity` | `cust_order_line.total_usr_ship_qty` | Shipped quantity. |
| `SalesOrderLine/ShippedBaseUOMQuantity` | `cust_order_line.total_shipped_qty` | Base shipped quantity. |
| `SalesOrderSchedule/*` | `cust_line_del.*` | Delivery schedule line behavior. |
| `SalesOrderLine/UserArea/Property/NameValue` | `cust_order_line.user_*`, `customer_part_id` | Custom and customer-specific identifiers. |

## ItemMaster

### Why keep it

This is the best inbound reference for part master plus site-specific planning and status behavior.

### Header fields worth keeping

| BOD path | VISUAL source | Why it matters |
| --- | --- | --- |
| `ItemMasterHeader/ItemID/ID` | `part.mfg_part_id` with `part.mfg_name` as scheme agency | External manufacturer part identity. |
| `ItemMasterHeader/Description` | `part.description` | Core part description. |
| `ItemMasterHeader/Note` | `notation.note` | Part notes. |
| `ItemMasterHeader/Classification/Codes/Code` | `part.abc_code`, `commodity_code`, `product_code`, `hts_code`, `material_code`, `nmfc_code`, `tariff_code`, `vat_code`, `drawing_id`, `revision_id`, `price_group`, `stage_id`, `drawing_rev_no`, `mro_class` | Important classification payload area. |
| `ItemMasterHeader/Type` | `part_site.primary_loc_id` | If primary location is `EAM`, the item becomes type `MRO`. |
| `ItemMasterHeader/LeadTimeDuration` | `part.planning_leadtime` | Planning lead time. |
| `ItemMasterHeader/BackFlushedIndicator` | `part.auto_backflush` | Auto-issue behavior. |
| `ItemMasterHeader/TrackingIndicator` | `part.stocked` | Stocked vs non-stocked. |
| `ItemMasterHeader/ItemStatus/Code` | `part.status`, `part.inventory_locked` | Obsolete vs hold semantics. |
| `ItemMasterHeader/BaseUOMCode` | `part.stock_um` | Base U/M behavior. |
| `ItemMasterHeader/ShippingUOMCode` | `part.weight_um` | Shipping/weight U/M behavior. |
| `ItemMasterHeader/DrawingAttachment/FileName` | `part.drawing_file` | Drawing file link. |
| `ItemMasterHeader/ProcurementParameters/*` | `part.order_policy`, `order_point`, `minimum_order_qty`, `maximum_order_qty`, `safety_stock_qty`, `fixed_order_qty`, `days_of_supply` | Core planning controls. |
| `ItemMasterHeader/Substitutions/Components/ItemID` | `part_substitute.substitute_part_id` | Substitute-part reference. |
| `ItemMasterHeader/AddOns/ItemID/ID` | `part_cross_selling.cross_sell_part_id` | Cross-sell/add-on reference. |

### ItemLocation fields worth keeping

| BOD path | VISUAL source | Why it matters |
| --- | --- | --- |
| `ItemLocation/Facility/IDs/ID` | `part_site.site_id` | Site-qualified item processing depends on this. |
| `ItemLocation/Classification/Codes/Code` | `part_site.*` plus primary warehouse/location and MRO indicator | Site-specific classification. |
| `ItemLocation/ItemStatus/Code` | `part_site.status`, `part_site.inventory_locked` | Site-level hold/obsolete behavior. |
| `ItemLocation/BackFlushedIndicator` | `part_site.auto_backflush` | Site-level auto issue. |
| `ItemLocation/TrackingIndicator` | `part_site.stocked` | Site-level stocked behavior. |
| `ItemLocation/LeadTimeDuration` | `part_site.planning_leadtime` | Site-level planning lead time. |
| `ItemLocation/ProcurementParameters/*` | `part_site.order_policy`, `order_point`, min/max, safety stock, EOQ, days of supply | Site-level planning controls. |
| `ItemLocation/ItemID/ID` with `@schemeAgencyID` | `vendor_part.vendor_id`, `vendor_part.vendor_part_id` | Vendor-part cross-reference behavior. |

## Location

### Why keep it

This section explains the identifier shapes that show up throughout the other BODs.

### ID patterns worth remembering

| Location type | ID shape |
| --- | --- |
| Office/company | `Office ~ <location or entity>` |
| Site | `Site ~ <site>` or `Office ~ <entity> ~ <site>` |
| Warehouse | `Warehouse ~ <warehouse>` or `Warehouse ~ <warehouse><addr_no>` |
| Ship-to | usually based on ship-to ID plus address number |

### Important notes

- IDs beginning with `Office` and having three elements are treated as sites.
- Warehouse and ship-to identity patterns are used repeatedly in `SalesOrder`, `PurchaseOrder`, `ReceiveDelivery`, and related flows.
- The location section is mainly useful as an ID-decoding reference, not for day-to-day field mapping.

## CustomerPartyMaster and ShipToPartyMaster

### Why keep them

They are the best concise inbound references for customer master and ship-to master behavior.

### CustomerPartyMaster highlights

| BOD path | VISUAL source | Why it matters |
| --- | --- | --- |
| `PartyIDs/ID` | `customer.id` | Customer identity. |
| `PartyIDs/TaxID` | `customer.tax_id_number` | Tax identifier. |
| `Name` | `customer.name` | Sold-to name. |
| `Location/Address/*` | `customer.addr_*`, `city`, `state`, `country`, `zipcode` | Core customer address mapping. |
| `PaymentTermID` | customer default terms fields | Terms sync reference. |
| `UserArea/Property/NameValue` | `customer.user_*`, `udf_layout_id`, `free_on_board`, `ship_via`, `customer_site.site_id`, `customer_site.customer_type` | Preserves customer-site semantics. |
| `Status/Code` | `customer.active_flag` | Active/inactive behavior. |
| `CurrencyCode` | `customer.currency_id` | Currency mapping. |
| `SalesPersonReference/IDs/ID` | `SalesRep ~ customer.salesrep_id` | Sales rep mapping. |

### ShipToPartyMaster highlights

| BOD path | VISUAL source | Why it matters |
| --- | --- | --- |
| `PartyIDs/ID` | `customer.id + shipto_id` | Ship-to identity shape. |
| `Name` | `customer.name` | Sold-to fallback. |
| `Location/Name` | `customer.name` or `cust_address.name` | Primary vs alternate ship-to naming. |
| `Location/Address/*` | `cust_address.addr_*` and parsed line-1 variants | Ship-to address mapping. |

## Useful Later

### PurchaseOrder

Keep this section for later if supply-side integration grows.

Most useful references:

- `purchase_order.id`, `vendor_id`, `warehouse_id`, `carrier_id`, `currency_id`, `terms_id`
- `purc_order_line.part_id`, `service_id`, `user_order_qty`, `order_qty`, `unit_price`
- demand assignment via `demand_supply_link.*` back to production-order operations and requirement pieces
- delivery schedule behavior through `purc_line_del.*`

### ReceiveDelivery

Keep for receipt-side material movement.

Most useful references:

- `receiver.id`, `receiver.received_date`, `receiver_line.warehouse_id`, `orig_country_id`, `carrier_id`, `bol_id`
- purchase-order linkage through `receiver_line.purc_order_id` and line numbers
- received vs returned quantity behavior

### InventoryAdjustment

Keep for future inventory transaction sync.

Most useful references:

- `inventory_trans.transaction_id`, `transaction_date`, `description`, `warehouse_id`, `part_id`, `qty`, `adj_reason_id`
- traceability linkage through `trace_inv_trans.trace_id` and quantity

### InspectionOrder

Keep for receiving inspection workflows.

Most useful references:

- `inspection_order.id`
- `receiver.received_date`
- `receiver_line.line_no`, `user_received_qty`
- purchase-order linkage through `receiver_line.purc_order_id` and `purc_order_line.order_qty`

### Requisition

Keep only as a pointer for later procurement work.

The retained value is that it maps inbound requisition identity and date from `purc_requisition.id` and `purc_requisition.requisition_date`.

### Quote

Keep only as a pointer for future quoting work.

The retained value is that it maps quote identity, status, customer/contact data, line pricing, and quantity-break pricing from `quote`, `quote_line`, and `quote_price`.

## Omitted on Purpose

These sections were not worth keeping in detailed form for current work:

- `BillToPartyMaster`
- `CodeDefinition`
- `ContactMaster`
- `Personnel`
- `Shipment`
- `SupplierPartyMaster`

They are not currently central to the repo’s Infor Visual research or the likely next implementation steps.

## Practical Use Guidance

When working in this repo, use this file as:

- an identifier-shape reference
- a status-translation reference
- a table-to-BOD crosswalk for work orders, operations, requirements, customer orders, items, and locations

Do not use this file as a complete vendor specification. If a future task needs edge-case behavior or rarely used elements, recover the older raw revision from git history and inspect the original section directly.