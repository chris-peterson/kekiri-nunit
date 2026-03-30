---
name: kekiri-migrate
description: Migrate BDD test fixtures to modern Kekiri (NUnit or xUnit). Supports two source frameworks — the legacy kekiri-nunit package (attribute-based [Given]/[When]/[Then] with Test base class) and the Kekiri.NUnit runner (fluent API with Scenarios base class, constructor-driven). Use this when migrating tests between kekiri versions, converting old Kekiri scenarios, or working on test files that inherit from Test, AutofacTest, or Kekiri.NUnit.Scenarios.
---

# Kekiri Migration

Migrate BDD test fixtures to modern Kekiri from either of two NUnit-based source frameworks. The target can be **Kekiri.Xunit** or **modern Kekiri.NUnit** — both share the same fluent API via `ScenarioBase`. The choice of test runner is a project decision, not a migration concern.

## Identify Your Source Framework

Before migrating, determine which framework you're migrating from — the transformations differ.

### Path A: kekiri-nunit (attribute-based)

Your tests inherit from `Test` (or `IoCTest` / `AutofacTest`) and use `[Given]`, `[When]`, `[Then]` attributes on methods. Steps are discovered via reflection.

```csharp
using Kekiri;

[Scenario(Feature.Calculator)]
public class Adding_two_numbers : Test
{
    [Given]
    public void Given_a_calculator() { _calc = new Calculator(); }

    [When]
    public void Adding() { _result = _calc.Add(1, 2); }

    [Then]
    public void The_result_is_3() { Assert.AreEqual(3, _result); }
}
```

**Identifying markers:** `[Given]`/`[When]`/`[Then]` attributes, `: Test` or `: AutofacTest` base class, `using Kekiri;` (not `Kekiri.NUnit`).

### Path B: Kekiri.NUnit (fluent, constructor-driven)

Your tests inherit from `Kekiri.NUnit.Scenarios` (or `Scenarios<TContext>`) and use the fluent API in the **constructor**. Steps are method references, not attribute-decorated.

```csharp
using Kekiri.NUnit;

public class Adding_two_numbers : Scenarios
{
    public Adding_two_numbers()
    {
        Given(a_calculator);
        When(adding);
        Then(the_result_is_3);
    }

    public void a_calculator() { _calc = new Calculator(); }
    public void adding() { _result = _calc.Add(1, 2); }
    public void the_result_is_3() { _result.Should().Be(3); }
}
```

**Identifying markers:** `using Kekiri.NUnit;`, `: Scenarios` or `: Scenarios<T>` base class, fluent `Given()`/`When()`/`Then()` calls in the constructor.

### Target: Kekiri.Xunit

Both paths converge to the same target — `[Scenario]` methods with fluent API:

```csharp
using Kekiri.Xunit;

public class Adding_two_numbers : Scenarios
{
    [Scenario]
    public void Adding_two_numbers_scenario()
    {
        Given(a_calculator);
        When(adding);
        Then(the_result_is_3);
    }

    private void a_calculator() { _calc = new Calculator(); }
    private void adding() { _result = _calc.Add(1, 2); }
    private void the_result_is_3() { _result.Should().Be(3); }
}
```

---

## Path A: kekiri-nunit → Kekiri.Xunit

This is the larger migration. You're changing both the step wiring model (attributes → fluent) and the test runner (NUnit → xUnit).

### Workflow

```text
1. Change base class (Test/AutofacTest → Scenarios/Scenarios<TContext>)
2. Remove [Given]/[When]/[Then] attributes, wire steps via fluent API in a [Scenario] method
3. Convert [Example] on class → [ScenarioOutline] + [Example] on method
4. Convert [When, Throws] → When(method).Throws()
5. Update IoC bootstrap ([SetUpFixture] → ICollectionFixture<T>)
6. Convert Step classes (Execute → ExecuteAsync)
7. Build and run tests
```

### A1. Attributes → Fluent API

**Before:**
```csharp
[Scenario(Feature.Calculator)]
public class Adding_two_numbers : Test
{
    [Given]
    public void Given_a_calculator() { _calc = new Calculator(); }

    [Given]
    public void The_user_enters_50() { _calc.Enter(50); }

    [Given]
    public void Next_the_user_enters_70() { _calc.Enter(70); }

    [When]
    public void When_the_user_presses_add() { _result = _calc.Add(); }

    [Then]
    public void The_screen_should_display_result_of_120() { Assert.AreEqual(120, _result); }
}
```

**After:**
```csharp
public class Adding_two_numbers : Scenarios
{
    [Scenario]
    public void Adding_two_numbers_scenario()
    {
        Given(a_calculator)
            .And(the_user_enters, 50)
            .And(the_user_enters, 70);
        When(pressing_add);
        Then(the_screen_displays, 120);
    }

    private void a_calculator() { _calc = new Calculator(); }
    private void the_user_enters(int n) { _calc.Enter(n); }
    private void pressing_add() { _result = _calc.Add(); }
    private void the_screen_displays(int expected) { _result.Should().Be(expected); }
}
```

**What changed:**
- `[Scenario(Feature.X)]` class attribute → removed
- `[Given]`/`[When]`/`[Then]` attributes → removed; methods wired in `[Scenario]` method
- Multiple `[Given]` methods → `.And()` chaining
- Parameterless methods with hardcoded values → parameterized methods via fluent API
- Step methods `public` → `private`

### A2. Chaining (And/But)

In kekiri-nunit, multiple `[Given]` methods are discovered in declaration order and reported as "And" automatically. In xUnit, chaining is explicit:

```csharp
Given(first_step)
    .And(second_step)
    .And(third_step)
    .But(exception_step);
```

Same applies to `Then`:
```csharp
Then(first_assertion)
    .And(second_assertion)
    .But(negative_assertion);
```

### A3. Data-Driven Tests (Scenario Outlines)

**Before:**
```csharp
[Example(12, 5, 7)]
[Example(20, 5, 15)]
[Scenario(Feature.Cucumber)]
public class Eating_cucumbers : Test
{
    private readonly int _start, _eat, _left;

    public Eating_cucumbers(int start, int eat, int left)
    {
        _start = start; _eat = eat; _left = left;
    }

    [Given]
    public void Given_there_are_START_cucumbers() { _cucumbers = _start; }

    [When]
    public void When_I_eat_EAT_cucumbers() { _cucumbers -= _eat; }

    [Then]
    public void I_should_have_LEFT_cucumbers() { Assert.AreEqual(_left, _cucumbers); }
}
```

**After:**
```csharp
public class Eating_cucumbers : Scenarios
{
    private int _cucumbers;

    [ScenarioOutline]
    [Example(12, 5, 7)]
    [Example(20, 5, 15)]
    public void Eating_cucumbers_scenario(int start, int eat, int left)
    {
        Given(there_are_START_cucumbers, start);
        When(I_eat_EAT_cucumbers, eat);
        Then(I_should_have_LEFT_cucumbers, left);
    }

    private void there_are_START_cucumbers(int start) { _cucumbers = start; }
    private void I_eat_EAT_cucumbers(int eat) { _cucumbers -= eat; }
    private void I_should_have_LEFT_cucumbers(int left) { _cucumbers.Should().Be(left); }
}
```

**What changed:**
- `[Example]` moves from class to method
- `[ScenarioOutline]` added (maps to xUnit `[Theory]`)
- Constructor parameters → method parameters
- Parameters passed through fluent API to step methods

### A4. Exception Handling

**Before:**
```csharp
[When, Throws]
public void When_dividing_by_zero() { _calc.Divide(1, 0); }

[Then]
public void It_throws_DivideByZeroException() { Catch<DivideByZeroException>(); }
```

**After:**
```csharp
When(dividing_by_zero).Throws();

private void it_throws() { var ex = Catch<DivideByZeroException>(); }
```

`[When, Throws]` → `When(method).Throws()`. `Catch<T>()` now returns the exception.

### A5. IoC / Autofac

**Before:**
```csharp
[SetUpFixture]
public class Bootstrap
{
    [OneTimeSetUp]
    public void Setup() { AutofacBootstrapper.Initialize(); }
}

public class Orchestration_test : AutofacTest
{
    [Given]
    public void Given_a_fake() { Container.Register(new FakeDataAccess()); }

    [When]
    public void Resolving() { _sut = Container.Resolve<Orchestrator>(); }

    [Then]
    public void It_uses_the_fake() { _sut.DataAccess.Should().BeOfType<FakeDataAccess>(); }
}
```

**After (ICollectionFixture):**
```csharp
public class AutofacFixture : IDisposable
{
    public AutofacFixture() => AutofacBootstrapper.Initialize();
    public void Dispose() { }
}

[CollectionDefinition("AutofacCollection")]
public class AutofacCollection : ICollectionFixture<AutofacFixture> { }

[Collection("AutofacCollection")]
public class Orchestration_test : Scenarios
{
    [Scenario]
    public void Using_fakes_scenario()
    {
        Given(a_fake);
        When(resolving);
        Then(it_uses_the_fake);
    }

    private void a_fake() { Container.Register(new FakeDataAccess()); }
    private void resolving() { _sut = Container.Resolve<Orchestrator>(); }
    private void it_uses_the_fake() { _sut.DataAccess.Should().BeOfType<FakeDataAccess>(); }
}
```

**After (BeforeAsync):**
```csharp
public class ExampleScenarios : Scenarios
{
    protected override async Task BeforeAsync()
    {
        AutofacBootstrapper.Initialize();
        await base.BeforeAsync();
    }
}
```

Both approaches work. `ICollectionFixture` bootstraps once per collection; `BeforeAsync` runs per scenario.

### A6. Step Classes

**Before:**
```csharp
public class FakeDataStep : Step
{
    public override void Execute()
    {
        Container.Register(new FakeDataAccess());
    }
}
```

**After:**
```csharp
public class Fake_data_access : Step<MyContext>
{
    public override async Task ExecuteAsync()
    {
        Container.Register(new FakeDataAccess());
        await Task.CompletedTask;
    }
}

// Referenced via:
Given<Fake_data_access>();
When<Resolving_an_instance>();
Then<It_uses_reals>()
    .But<data_access_is_fake>();
```

`Step` → `Step<TContext>`, `Execute()` → `ExecuteAsync()`.

### A7. Lifecycle Hooks

**Before:**
```csharp
protected override void SetupScenario() { /* before */ }
protected override void CleanupScenario() { /* after */ }
```

**After:**
```csharp
protected override Task BeforeAsync() { /* before */ return Task.CompletedTask; }
protected override Task AfterAsync() { /* after */ return Task.CompletedTask; }
```

### A8. Async Steps

kekiri-nunit requires step methods to be `void`. Kekiri.Xunit supports async:

```csharp
Given(setup)
    .AndAsync(async_setup);
WhenAsync(async_action);
ThenAsync(async_assertion);
```

---

## Path B: Kekiri.NUnit → Kekiri.Xunit

This is the smaller migration. The fluent API is identical — you're only moving steps from the constructor to a `[Scenario]` method, and switching the test runner.

### Workflow

```text
1. Change using (Kekiri.NUnit → Kekiri.Xunit)
2. Move Given/When/Then from constructor to a [Scenario] method
3. Convert [Example] on class → [ScenarioOutline] + [Example] on method
4. Update IoC bootstrap ([SetUpFixture] → ICollectionFixture<T>)
5. Optionally: migrate Container.Register/Resolve to typed Context
6. Build and run tests
```

### B1. Constructor → [Scenario] Method

**Before:**
```csharp
using Kekiri.NUnit;

public class Cart_exists : Scenarios
{
    public Cart_exists()
    {
        Given(an_authenticated_user)
            .And(customer_has_existing_cart);
        When(getting_the_cart);
        Then(the_cart_is_returned);
    }

    public void an_authenticated_user() { /* ... */ }
    public void customer_has_existing_cart() { /* ... */ }
    public void getting_the_cart() { /* ... */ }
    public void the_cart_is_returned() { /* ... */ }
}
```

**After:**
```csharp
using Kekiri.Xunit;

public class Cart_exists : Scenarios
{
    [Scenario]
    public void Cart_exists_scenario()
    {
        Given(an_authenticated_user)
            .And(customer_has_existing_cart);
        When(getting_the_cart);
        Then(the_cart_is_returned);
    }

    private void an_authenticated_user() { /* ... */ }
    private void customer_has_existing_cart() { /* ... */ }
    private void getting_the_cart() { /* ... */ }
    private void the_cart_is_returned() { /* ... */ }
}
```

**What changed:**
- `using Kekiri.NUnit` → `using Kekiri.Xunit`
- Constructor body → `[Scenario]` method
- Step methods `public` → `private`
- The fluent Given/When/Then calls are **identical** — just moved

### B2. Data-Driven Tests

**Before:**
```csharp
[Example("input1", "expected1")]
[Example("input2", "expected2")]
public class My_scenario : Scenarios
{
    public My_scenario(string input, string expected)
    {
        _input = input; _expected = expected;
        Given(some_setup);
        When(the_action);
        Then(the_result);
    }
}
```

**After:**
```csharp
public class My_scenario : Scenarios
{
    [ScenarioOutline]
    [Example("input1", "expected1")]
    [Example("input2", "expected2")]
    public void My_scenario_outline(string input, string expected)
    {
        Given(some_setup, input);
        When(the_action);
        Then(the_result, expected);
    }
}
```

`[Example]` moves from class to method, constructor params become method params, `[ScenarioOutline]` added.

### B3. IoC Bootstrap

**Before (NUnit):**
```csharp
[SetUpFixture]
public class Bootstrap
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        AutofacBootstrapper.Initialize(c => { /* ... */ });
    }
}
```

**After (xUnit):**
```csharp
public class AutofacFixture : IDisposable
{
    public AutofacFixture() => AutofacBootstrapper.Initialize(a => { /* ... */ });
    public void Dispose() { }
}

[CollectionDefinition("AutofacCollection")]
public class AutofacCollection : ICollectionFixture<AutofacFixture> { }
```

Every test class that needs Autofac gets `[Collection("AutofacCollection")]`.

### B4. Container → Typed Context (Optional)

With Kekiri.NUnit, many tests use `Container.Register()` and `Container.Resolve<T>()` directly. In Kekiri.Xunit, you can optionally migrate to a typed Context class where dependencies are constructor-injected by Autofac:

**Before:**
```csharp
public class MyTestBase : Scenarios
{
    protected MyTestBase()
    {
        Container.Register(new FakeRepository());
        Container.Register(new FakeClient());
    }

    protected void doing_the_thing()
    {
        var controller = Container.Resolve<MyController>();
        _result = controller.DoThing();
    }
}
```

**After:**
```csharp
public class MyContext
{
    public FakeRepository FakeRepository { get; }
    public FakeClient FakeClient { get; }
    public MyController Controller { get; }

    public MyContext(FakeRepository repo, FakeClient client, MyController controller)
    {
        FakeRepository = repo;
        FakeClient = client;
        Controller = controller;
    }
}

[Collection("AutofacCollection")]
public class MyTestBase : Scenarios<MyContext>
{
    protected void doing_the_thing()
    {
        _result = Context.Controller.DoThing();
    }
}
```

This is optional — `Container.Register()` / `Container.Resolve<T>()` work in both frameworks. Typed Context is cleaner for large test suites because dependencies are explicit and constructor-injected.

### B5. Exception Handling

`.Throws()` and `Catch<T>()` work identically in both frameworks. No changes needed — just move them from the constructor to the `[Scenario]` method along with everything else.

---

## Partial Migration (Resuming In-Progress Work)

In real codebases, migration is often incremental. The target xUnit project may already contain tests — some migrated previously, some written directly in xUnit. Before migrating a directory, check for existing work.

### Mindset: cherry-pick, don't wholesale migrate

The goal is not to move every old test into the new project. The goal is to ensure the new project has sufficient coverage. The old tests reflect the old infrastructure's constraints and idioms — porting them line-by-line reproduces those constraints in a place they don't belong.

Instead, read what each old test **asserts** (its intent), then check whether the new test suite already covers that intent. If it does, the old test has nothing to contribute. If it doesn't, write a new test that reaches the same assertion using the new base class's step methods as they were designed to be used. Don't translate the old test's setup sequence — it reflects wiring that no longer applies.

### Step 1: Inventory both sides

Compare the source (NUnit) and target (xUnit) directories. Match by scenario **intent** (what is being asserted), not file path or class name — the directory structure and naming may differ.

```text
Source: Tests/OldProject/Service/Orders/
Target: Tests/NewProject/Service/Orders/
```

Categorize each source test:
- **Already covered** — the new suite asserts the same behavior (possibly under a different name or consolidated into a `[ScenarioOutline]`). Skip it.
- **Unique coverage** — the old test asserts something the new suite doesn't. Worth bringing over.
- **Obsolete** — the old test validates behavior that's been removed, replaced, or is no longer relevant. Drop it.

### Step 2: Adopt the existing base class and Context

If the target directory already has a base class and Context, use them. Do not create a second base class or Context — that fragments the test infrastructure.

Read the existing base class carefully before writing anything. Understand not just what step methods exist, but how they compose — steps may have side effects (setting internal flags, registering permissions) that affect downstream behavior. Use existing composite steps as-is rather than reconstructing their internals from the old test's approach.

If the existing Context is missing dependencies your scenarios need, extend it — add properties and constructor parameters, don't create a parallel context.

### Step 3: Write new tests for uncovered intent

For each old test with unique coverage:
1. Create the file in the target directory
2. Inherit from the existing base class
3. Write the `[Scenario]` method using the new base class's steps naturally — compose them as the new infrastructure intends, not as the old test's setup sequence dictated
4. Build and run

### Step 4: Verify no duplication

After migrating, check that you haven't introduced duplicate test coverage. Two tests asserting the same thing under the same conditions is waste, not safety.

---

## Validation

Run tests frequently during migration — after each base class + scenario group, not at the end:

```bash
dotnet test --filter "FullyQualifiedName~<Namespace>"
```

A red test right after migration is easy to fix; a red test discovered 20 files later is painful to trace.

## Common Pitfalls

- **Path A: Forgetting to remove `[Given]`/`[When]`/`[Then]`** — they don't exist in Kekiri.Xunit. The compiler catches this.
- **Path B: Leaving Given/When/Then in the constructor** — they must be in a `[Scenario]` method or they won't run as a test.
- **Using `[Scenario]` for data-driven tests** — use `[ScenarioOutline]` with `[Example]`, otherwise only one data set runs.
- **Missing `.Throws()`** — if the old code expected an exception (`[When, Throws]` or `When(...).Throws()` in constructor), the `.Throws()` call must be present.
- **Missing `[Collection("...")]`** — when using `ICollectionFixture` for Autofac bootstrap, every test class needs the `[Collection]` attribute or the container won't be initialized.
- **Multiple `[When]`** — both frameworks enforce exactly one When per scenario.
