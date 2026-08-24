# RCS 3.0 Standard Architecture & Coding Guidelines

You are developing the SIASUN RCS 3.0 standard product, which must gracefully adapt to historical project complexities (NXP ERACK, NXP Tianjin Molding, TXC Crystal) and future deployments. 

Always adhere to the following 7 core architectural constraints:

## 1. Workflow Engine over State Machines
- **DO NOT** attempt to build a generic DAG engine or a giant unified state machine (like Molding's 22-state switch).
- **DO** use the TXC-style `TaskWorkflow` stepping engine. 
- The domain model (`AgvTask`) should only expose a 5-state coarse lifecycle (`Pending`, `Running`, `Succeeded`, `Failed`, `Canceled`). 
- Fine-grained execution is strictly driven by `StepIndex`, `WaitingEvent`, and `ActiveLeg`.

## 2. OptionCode Schema-Driven Encoding
- **DO NOT** use hardcoded bit-packing (like ERACK's `TaskCode1/2`).
- **DO** use the TXC-style Schema-driven approach (`OptionCodeSchema`, `Assembler`, `Encoder`, `Decoder`).
- Ensure it supports reverse-decoding for frontend displays and multiple schema versions (`erack.v1`, `molding.v1`, etc.).

## 3. TM Callback Tracking (`TaskSerialRegistry`)
- **DO NOT** use string replacement hacks (e.g., `.Replace("0_fetch", "")`) to find internal tasks from TM callbacks.
- **DO** use a dedicated `TaskSerialRegistry` to map TM sequence numbers and `AgvSerial` to internal RCS tasks, handling multi-leg scenarios (Fetch/Put1/Put2) safely.

## 4. Hardware/PLC Layer as Optional Plugins
- **DO NOT** build PLC S7/Modbus polling directly into the core platform flow.
- **DO** abstract hardware interactions behind `IHardwareGate` (Ports and Adapters). PLC logic is highly project-specific (e.g., twin-arm sync, slot management) and must remain an optional plug-in.

## 5. Inbound Ports and Adapters
- **DO NOT** force a single unified Inbound API for upstream systems.
- **DO** use the Ports and Adapters pattern (`IInboundPort`, `IOutboundAdapter`). Upstream systems vary drastically (REST for AMA/MES, WCF/SOAP for Mica WMS) and have different responsibilities.

## 6. Batch & Multi-AGV Orchestration
- The domain model must anticipate Batch Management and Multi-Vehicle orchestration.
- Scenarios like NXP Molding require splitting one batch into multiple tasks and coordinating their dispatch logic.

## 7. Domain Events for Side Effects
- **DO NOT** use procedural method calls for cross-domain side effects (e.g., calling MES API directly from task completion).
- **DO** emit local domain events (e.g., `TaskLifecycleEndedEvent`) and handle them via Event Handlers to ensure loose coupling.
