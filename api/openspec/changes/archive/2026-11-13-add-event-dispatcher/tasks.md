# Tasks

## 1. Application: contracts and dispatcher

- [x] 1.1 Add `IEventHandler<in TEvent> where TEvent : IDomainEvent` and `IEventDispatcher` in `src/WeightTracker.Application/Common/Dispatcher` (design §1); verify `dotnet build WeightTracker.slnx` passes and `WeightTracker.Application.csproj` has no new package reference
- [x] 1.2 Implement `EventDispatcher` with exact-type resolution, a cached typed invoker and sequential loops without `try`/`catch` (design §2–§4); verify the build passes
- [x] 1.3 Add `test/WeightTracker.Unit/Application/Common/Dispatcher/EventDispatcherTests.cs` with a hand-built `ServiceCollection` covering every scenario of the spec requirements "Dispatch runs every handler for the event's exact type", "Events without handlers are ignored", "Dispatch is sequential and ordered" and "Handler failures propagate and stop the dispatch". The failure test asserts the thrown instance is the handler's own exception, not a wrapper. Verify these tests pass with `dotnet test test/WeightTracker.Unit`
- [x] 1.4 Review: compare the new Application files and tests with `src/WeightTracker.Application/Common/IUnitOfWork.cs` and `test/WeightTracker.Unit/Application/Services/*Tests.cs` (namespace, doc comments, naming, Arrange / Act / Assert); verify there are no deviations other than those recorded in the design

## 2. Api: automatic handler registration

- [x] 2.1 Add `EventHandlerRegistration.AddEventHandlers(params Assembly[])` in `src/WeightTracker.Api/Common` (design §5); verify the build passes
- [x] 2.2 Call `builder.Services.AddEventHandlers(typeof(IUnitOfWork).Assembly, typeof(UnitOfWork).Assembly)` in `Program.cs` next to the other service registrations; verify the build passes and `dotnet run --project src/WeightTracker.Api` starts
- [x] 2.3 Add a `WeightTracker.Api` project reference to `test/WeightTracker.Unit/WeightTracker.Unit.csproj`; verify the unit project builds
- [x] 2.4 Add `test/WeightTracker.Unit/Api/Common/EventHandlerRegistrationTests.cs` covering spec requirement "Declared handlers are registered automatically" (no explicit registration, two event types, abstract and open generic skipped). Add one test that `IEventDispatcher` resolves and a scanned handler runs on dispatch. Verify these tests pass
- [x] 2.5 Review: compare `EventHandlerRegistration` with `src/WeightTracker.Api/Common/ExceptionMapper.cs` (namespace, doc comment style) and check `Program.cs` registers no handler by hand; verify by reading the diff

## 3. Invariants

- [x] 3.1 Rewrite the "Domain events: dormant" section of `.stpr/template/STPR_INVARIANTS.md` as described in design §7; verify the section no longer says that no dispatcher exists and still forbids calling `AddDomainEvent` until dispatch is wired
- [x] 3.2 Run the template parity check from `AGENTS.md`; verify it prints no `DRIFT:` line

## 4. Full verification

- [x] 4.1 Run `dotnet build WeightTracker.slnx` (warnings are errors) and `dotnet test WeightTracker.slnx` with Docker running; verify both pass, including the integration suite
- [x] 4.2 Walk through the `AGENTS.md` quality checklist items that apply (Program.cs registration, tests split into Arrange / Act / Assert, no duplicated code); verify each applicable item holds

## Workflow follow-up

- Archive the change after review, then confirm `openspec/specs/domain-event-dispatch/spec.md` was created with its Purpose.
