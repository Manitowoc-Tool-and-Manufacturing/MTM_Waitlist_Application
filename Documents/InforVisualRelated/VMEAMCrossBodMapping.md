# VISUAL-EAM Cross BOD Working Reference

## Glossary

| Term | Meaning |
| --- | --- |
| `BOD` | Business Object Document, an Infor message format used to move business data between systems. |
| `EAM` | Enterprise Asset Management, the asset and maintenance system on the non-VISUAL side of this document. |
| `VISUAL` | Infor VISUAL, the ERP and manufacturing system referenced throughout this file. |
| `Inbound` | Data coming into a system from another system. |
| `Outbound` | Data leaving a system and being sent to another system. |
| `MRO` | Maintenance, repair, and operations inventory or parts. |
| `UOM` | Unit of measure, such as each, pounds, or hours. |

This file is a curated working extract of the older VISUAL-to-EAM cross-BOD mapping guide.

It removes:

- legal boilerplate and publication filler
- page headers, pagination, and OCR formatting damage
- repeated low-value line-by-line field prose
- large blocks of integration detail that are not relevant to this repo right now

It keeps what is useful now and what is likely to matter later if the VISUAL and EAM integration becomes part of active implementation work.

## Direction Legend

| Marker | Meaning |
| --- | --- |
| `►►►` | VISUAL publishes or sends to EAM |
| `◄◄◄` | EAM publishes or sends to VISUAL |

## Scope

The most useful cross-system subjects for this repo are:

- work orders and production requests
- parts and MRO item profiles
- purchasing, receiving, and inventory movement
- shop resources and constrained resources
- shared code lists and translation tables

## Priority Map

| Priority | BOD | Why it matters |
| --- | --- | --- |
| Current | `ProductionOrder` | Best cross-map for `WORK_ORDER` identity, status translation, schedule dates, and EAM production request behavior. |
| Current | `ItemMaster` | Best cross-map for VISUAL parts and part-site data versus EAM parts/trades, classes, UOMs, and status behavior. |
| Current | `ReceiveDelivery` | Most important cross-map if shop-floor receiving, WO receipts/issues, or inbound material movement becomes part of the integration. |
| Current | `InventoryAdjustment` | Most direct bridge between VISUAL inventory transactions and EAM transaction lines. |
| Near-term | `PurchaseOrder` | Useful if vendor purchasing or supply-side demand assignment becomes active work. |
| Near-term | `ConstrainedResource` | Useful if EAM resources need to align with VISUAL shop resources and workcenters. |
| Near-term | `CodeDefinition` | Useful for keeping shared lists like UOMs, priorities, incoterms, and commodity codes aligned. |
| Future | `Requisition` | Useful if EAM requisitions feed VISUAL purchasing. |
| Future | `Shipment` | Useful if returns, issues, and shipping-side movements become part of the EAM bridge. |

## Core Cross-System Patterns

### Shared identity concepts

- `@accountingEntity` is consistently used to identify the EAM organization or the VISUAL accounting entity context.
- `@location` is consistently used to carry enterprise location context such as `Site ~ <site>` or `Warehouse ~ <warehouse>`.
- `@lid` identifies the source system instance and is built from the VISUAL instance ID.

### Common ID shapes

| Domain | Typical shape |
| --- | --- |
| Production order | `type ~ base_id ~ lot_id ~ split_id ~ 0` |
| Site location | `Site ~ <site_id>` |
| Warehouse location | `Warehouse ~ <warehouse_id>` |
| Receipt / shipment document | prefixed values such as `Receiver ~ ...`, `Shipper ~ ...`, `WOReceipt ~ ...` |

### Common modeling differences

| VISUAL | EAM |
| --- | --- |
| `PART` and service-like references are mostly separate VISUAL concepts | EAM often models these as part vs trade |
| Work orders use VISUAL status codes and scheduling fields | EAM production requests use EAM request/status fields |
| Shop resources come from `SHOP_RESOURCE` | EAM resources use `R5RESOURCES` |
| Inventory transactions are row-based in VISUAL | EAM uses transaction and transaction-line objects |

## CodeDefinition

### Why keep it

This section is the quickest way to understand which shared code lists the VISUAL-EAM bridge actually uses.

### Key takeaways

- VISUAL code lists are sourced primarily from `soa_dimension` and `soa_code_list`.
- EAM imports a focused set of list IDs rather than arbitrary code tables.
- Open/closed/deleted status behavior matters because EAM often treats anything non-open as inactive.

### Most useful list IDs preserved in the source

- production order priorities
- cost centers
- unit codes
- currency
- incoterms / FOB point
- freight terms
- payment methods
- payment terms
- transportation methods / ship via
- commodity codes
- qualifications

### Practical note

If a future task involves code translation mismatches, this section is the first place to check before touching application logic.

## ConstrainedResource

### Why keep it

This is the simplest bridge between VISUAL workcenter/resource records and EAM resource records.

### Core mapping

| BOD field | VISUAL | EAM |
| --- | --- | --- |
| `ConstrainedResource/ResourceID/ID` | `shop_resource.id` | `R5RESOURCES.RSS_CODE` |
| `ConstrainedResource/Description` | `shop_resource.description` | `R5RESOURCES.RSS_DESC` |
| `ConstrainedResource/ResourceTypeCode` | `shop_resource.type` | `R5RESOURCES.RSS_TYPE` |

### Practical note

This is the best quick reference if later work tries to align VISUAL workcenters or resources with EAM maintenance/resource planning records.

## ItemMaster

### Why keep it

This is the most useful cross-map for item identity, MRO classification, status, UOM, and part-vs-trade behavior.

### Direction summary

| Direction | Summary |
| --- | --- |
| `►►►` VISUAL to EAM | VISUAL part and part-site data can create or update EAM parts and trades. |
| `◄◄◄` EAM to VISUAL | EAM item identity, status, and classification can flow back into VISUAL item fields. |

### Core identity mapping

| BOD field | VISUAL | EAM |
| --- | --- | --- |
| `ItemMasterHeader/ItemID/ID` | `part.id`, `part.mfg_part_id`, or customer-pricing part ID depending on scheme | `r5parts.par_code` or `r5trades.trd_code` |
| `@accountingEntity` | `accounting_entity.id` | `r5parts.par_org` / `r5trades.trd_org` |
| `@lid` | VISUAL instance ID wrapper | source-system marker in the exchange |

### Part vs trade behavior

- VISUAL generally publishes item records as non-service by default.
- EAM distinguishes between parts and trades.
- Service indicator differences are important any time an integration flow can refer to either stocked items or service-like procurement lines.

### Classification behavior worth keeping

The source maps VISUAL classifications to EAM concepts such as:

- MRO classes
- equipment categories
- primary and secondary commodity codes
- hierarchy codes

VISUAL fields that matter most here:

- `part.abc_code`
- `part.commodity_code`
- `part.product_code`
- `part.hts_code`
- `part.material_code`
- `part.nmfc_code`
- `part.tariff_code`
- `part.vat_code`
- `part.drawing_id`
- `part.revision_id`
- `part.price_group`
- `part.stage_id`
- `part.drawing_rev_no`
- `part.mro_class`

### Status behavior worth keeping

The most important cross-system rule is that EAM status is not a simple one-to-one copy of VISUAL status.

Key behaviors:

- VISUAL obsolete and inventory-locked states affect what EAM treats as active vs out-of-service.
- EAM can return values that map back into VISUAL as open, hold, do-not-reorder, or deleted semantics.
- `Prevent Reorders` on the EAM side matters when interpreting status round-trips.

### UOM behavior worth keeping

| BOD field | VISUAL | EAM |
| --- | --- | --- |
| `BaseUOMCode` | `part.stock_um` | `r5parts.par_uom` |
| `StorageUOMCode` | `part.stock_um` | `r5parts.par_uom` |
| `ShippingUOMCode` | `part.weight_um` | `r5parts.par_uom` |

### MRO indicator behavior

If `part_site.primary_loc_id = 'EAM'`, the item is treated as type `MRO` in the outbound direction.

That is the key shortcut to remember when checking whether a VISUAL item is expected to participate in EAM item sync.

## ProductionOrder

### Why keep it

This is the most important VISUAL↔EAM work-order bridge.

It explains how VISUAL work orders align with EAM production requests.

### Core identity mapping

| BOD field | VISUAL | EAM |
| --- | --- | --- |
| `ProductionOrderHeader/DocumentID/ID` | `work_order.type ~ base_id ~ lot_id ~ split_id ~ 0` | `r5productionrequests.prq_code` |

### Status behavior worth keeping

#### VISUAL to EAM (`►►►`)

VISUAL status translation in the cross-map is more explicit than the plain inbound guide:

- unreleased `U` -> `Planned`
- firmed `F` -> `Firm`
- released `R` with transactions -> `Active`
- released `R` without transactions -> `Released`
- canceled `X` -> `Canceled`
- closed `C` -> `Closed`

Important note preserved from the source:

- inbound to EAM only accepts a subset of production-order states, especially `Firm` and `Canceled`, rather than every possible VISUAL lifecycle state.

#### EAM to VISUAL (`◄◄◄`)

- EAM production request statuses are filtered before outbound sync back to VISUAL.
- The source specifically notes that only selected EAM request states are sent back out.

### Schedule and date fields worth keeping

| BOD field | VISUAL | EAM |
| --- | --- | --- |
| `DocumentDateTime` | `work_order.create_date` | `r5productionrequests.prq_created` |
| `Status/EffectiveDateTime` | `work_order.status_eff_date` plus transaction timing | `r5productionrequests.prq_laststatusupdate` |
| `ExecutionTimePeriod/StartDateTime` | earliest work-order transaction timing | `r5productionrequests.prq_productionstart` |
| `ExecutionTimePeriod/EndDateTime` | `work_order.close_date` | `r5productionrequests.prq_productionend` |
| `ForecastedTimePeriod/StartDateTime` | `work_order.sched_start_date` or release date fallback | `r5productionrequests.prq_productionstart` |
| `ForecastedTimePeriod/EndDateTime` | `work_order.sched_finish_date` or desired-want fallback | `r5productionrequests.prq_productionend` |
| `DueDateTime` | `work_order.desired_want_date` | `r5productionrequests.prq_prodrequestend` |
| `EarliestStartDateTime` | `work_order.desired_rls_date`, `hard_release_date` | `r5productionrequests.prq_prodrequeststart` |

### Practical note

If future work needs to reconcile work-order lifecycle state between VISUAL and EAM, this is the first section to use.

## InventoryAdjustment

### Why keep it

This is the cleanest cross-map between VISUAL inventory transactions and EAM transaction lines.

### Direction summary

The preserved excerpt is primarily `◄◄◄` EAM-to-VISUAL oriented in the source file, but it still provides the most useful transaction-line crosswalk.

### Core mapping

| BOD field | VISUAL | EAM |
| --- | --- | --- |
| `DocumentID/@accountingEntity` | `accounting_entity.id` | `r5transactions.tra_org` |
| `DocumentID/@location` | `Site ~ warehouse.site_id` | enterprise location on transaction org |
| `DocumentID/ID` | `inventory_trans.transaction_id` | `r5translines.trl_trans` + `trl_line` |
| `DocumentDateTime` | `inventory_trans.transaction_date` | `r5translines.trl_date` |
| `Description` | `inventory_trans.description` | transaction description |
| `Line/WarehouseLocation/ID` | `inventory_trans.warehouse_id` | `r5translines.trl_tocode` |
| `Line/Item/ItemID/ID` | `inventory_trans.part_id` | `r5translines.trl_part` |
| `Line/SerializedLot/LotIDs/ID` | `trace_inv_trans.trace_id` | `r5translines.trl_lot` |
| `Line/Quantity` | `inventory_trans.qty` | `r5translines.trl_qty` |
| `Line/ReasonCode` | `inventory_trans.adj_reason_id` | `r5translines.trl_type` |

### Practical note

If future integration work needs transaction-level inventory movement instead of order-level procurement or receiving, this is the best section to start from.

## PurchaseOrder

### Why keep it

This is the best cross-map for vendor procurement flowing from VISUAL into EAM.

### Header fields worth keeping

| BOD field | VISUAL | EAM |
| --- | --- | --- |
| `DocumentID/@accountingEntity` | `purchase_order.site_id -> site.entity_id` | `r5orders.ord_org` |
| `DocumentID/@location` | `Site ~ purchase_order.site_id` | enterprise location |
| `DocumentID/ID` | `purchase_order.id` | `r5orders.ord_code` |
| `DocumentDateTime` | `purchase_order.order_date` | `r5orders.ord_date` |
| `Status/Code` | translated from VISUAL PO status | `r5orders.ord_rstatus` |
| `SupplierParty/PartyIDs/ID` | `purchase_order.vendor_id` | `r5orders.ord_supplier` |
| `ShipToParty/Location/ID` | `Warehouse ~ purchase_order.warehouse_id` | delivery address |
| `ExtendedAmount/@currencyID` | `purchase_order.currency_id` | `r5orders.ord_curr` |
| `CarrierParty/PartyIDs/ID` | `purchase_order.carrier_id` | `r5orders.ord_shipvia` |
| `TransportationTerm/IncotermsCode` | `purchase_order.free_on_board` | `r5orders.ord_fobpoint` |
| `PaymentTerm/Term/ID` | `purchase_order.terms_id` | `r5orders.ord_paymentterms` |
| `PromisedDeliveryDateTime` | `purchase_order.promise_date` | `r5orders.ord_due` |
| `PaymentMethodCode` | vendor payment method translation | `r5orders.ord_paymethod` |

### Line fields worth keeping

| BOD field | VISUAL | EAM |
| --- | --- | --- |
| `Line/LineNumber` | `purc_order_line.line_no` | `r5orderlines.orl_ordline` |
| `Line/Status/Code` | line or header PO status translation | `r5orderlines.orl_rstatus` |
| `Line/Item/ItemID/ID` | `part_id` or `service_id` | `r5orderlines.orl_part` or `orl_trade` |
| `Line/Item/ServiceIndicator` | based on `service_id` | part vs trade behavior |
| `Line/Quantity/@unitCode` | `purc_order_line.purchase_um` | `r5orderlines.orl_puruom` |
| `Line/Quantity` | `purc_order_line.user_order_qty` | `r5orderlines.orl_ordqty` |
| `Line/UnitPrice/Amount` | `purc_order_line.unit_price` | `r5orderlines.orl_price` |
| `Line/RequiredDeliveryDateTime` | desired receive date | `r5orderlines.orl_due` |
| `Line/ShipToParty/Location/ID` | warehouse on line or header fallback | delivery/store targets |
| `Line/RequisitionReference/*` | `purc_order_req.*` | requisition linkage in EAM |
| `Line/PromisedDeliveryDateTime` | line promise date or header fallback | `r5orderlines.orl_due` |

### Practical note

The requisition-reference lines matter because they explain how an EAM requisition can remain traceable once it becomes a VISUAL purchase order.

## ReceiveDelivery

### Why keep it

This is the most complex but also the most valuable cross-map if receiving, returns, WO receipts/issues, inter-branch movement, or consignment transactions become important.

### Supported transaction families preserved in the source

- `Receiver`
- `Shipper`
- `WOReceipt`
- `WOIssueRtn`
- `IBTReceiver`
- `CNSNReceiver` vendor and customer variants

### Core identity and location behavior

| BOD field | VISUAL | EAM |
| --- | --- | --- |
| `DocumentID/@accountingEntity` | `accounting_entity.id` | `r5transactions.tra_org` |
| `DocumentID/@location` | site from PO, SO, inventory transaction, IBT, or consignment source | enterprise location |
| `DocumentID/ID` | prefixed receiver/shipper/WO/IBT/consignment identity | `r5transactions.tra_dckcode` or `tra_code` |
| `DocumentDateTime` | receipt, shipment, inventory, or consignment date | `r5dockreceipts.dck_recvdate` / `r5transactions.tra_date` |
| `WarehouseLocation/ID` | warehouse from receipt/shipment/inventory source | `r5transactions.tra_tocode` |

### Item-level behavior worth keeping

| BOD field | VISUAL | EAM |
| --- | --- | --- |
| `ReceiveDeliveryItem/ItemID/ID` | item source depends on transaction family | `r5translines.trl_part` |
| `ServiceIndicator` | service only for certain PO receipt cases | part vs trade interpretation |
| `PurchaseOrderReference/*` | PO ID, line, and schedule linkage from receiver lines | `trl_order`, `trl_ordline`, schedule linkage |
| `DocumentReference` | work-order or asset reference for WO receipt/return scenarios | target object on EAM side |
| `ReceivedQuantity` | quantity source depends on family | `r5translines.trl_qty` |
| `SerializedLot/LotIDs/ID` | `trace_inv_trans.trace_id` or service trace | `trl_lot` |
| `LineNumber` | receiver/shipper/IBT/consignment line number | `trl_line` |

### Key practical notes

- For inbound, EAM may not directly use some document identity values and may generate its own transaction identifiers.
- The source explicitly notes that this noun can be used for both PO receipts and work-order-related inventory flows.
- This is the best cross-map for understanding how a VISUAL inventory transaction or receipt can point back to a production order, asset, or purchase order context in EAM.

## Requisition

### Why keep it

This is the cleanest preserved EAM-to-VISUAL purchasing demand reference.

### Core mapping worth keeping

| BOD field | VISUAL | EAM |
| --- | --- | --- |
| `RequisitionHeader/DocumentID/ID` | `purc_requisition.id` | `r5requisitions.req_code` |
| `RequisitionHeader/DocumentDateTime` | `purc_requisition.requisition_date` | `r5requisitions.req_date` |
| `RequisitionHeader/Status/Code` | translated from VISUAL requisition statuses | `r5requisitions.req_rstatus` |
| `RequisitionLine/LineNumber` | `purc_req_line.line_no` | `r5requislines.rql_reqline` |
| `RequisitionLine/Item/ItemID/ID` | `service_id` or `part_id` | `r5requislines.rql_part` or `rql_trade` |
| `RequisitionLine/ServiceIndicator` | based on service ID presence | service trade vs part type |
| `RequisitionLine/Quantity` | user order qty and purchase UM | `r5requislines.rql_qty`, `rql_uom` |
| `RequisitionLine/UnitPrice/Amount` | `purc_req_line.unit_price` | `r5requislines.rql_price` |
| `RequisitionLine/RequiredDeliveryDateTime` | desired receive date | `r5requislines.rql_due` |

### Practical note

If EAM-originated demand ever needs to become VISUAL purchasing, this section matters more than the basic PO section.

## Shipment

### Why keep it

This section is lower priority than `ReceiveDelivery`, but still matters for returns and issue-side flows.

### What to remember

- shipment IDs are family-specific, similar to `ReceiveDelivery`
- site and warehouse identity rules mirror the receiving side
- this section becomes more useful if the EAM bridge must handle WO issue, PO receipt return, or shipment-return scenarios

## What Was Intentionally Omitted

Detailed line-by-line retention was intentionally dropped for:

- hospitality-related code lists
- repetitive note-print-language permutations
- duplicated item note and comment mechanics
- long blocks of alternate address/store/delivery wording that repeat the same enterprise-location ideas

## Practical Use Guidance

Use this file as:

- a VISUAL↔EAM direction reference
- a high-level table-to-table crosswalk
- a status and identifier translation guide

Do not use it as the complete vendor specification. If a future task needs an obscure edge case, recover the older raw revision from git history and inspect that exact section directly.