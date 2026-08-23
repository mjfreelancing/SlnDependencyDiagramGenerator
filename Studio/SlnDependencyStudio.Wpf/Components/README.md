# Components

Reusable non-visual UI state helpers that manage interaction patterns shared across features.

These are not XAML controls (see `Controls/`), not domain models (`Features/Xxx/Models/`), and not view models (`Features/Xxx/XxxViewModel.cs`). They are lightweight state holders that bundle a bit of observable data with one or more commands — small, self-contained building blocks that a view model can compose.

## What belongs here

- State holders with `ReactiveCommand` or `IObservable` members
- Classes that manage a repeatable UI pattern (e.g., tag input, search-as-you-type, pagination)
- No XAML, no view binding — consumed by view models, bound by views

## What does NOT belong here

- Domain data classes (stay in `Features/Xxx/Models/`)
- XAML-based controls (stay in `Controls/`)
- Full view models (stay in `Features/Xxx/`)
