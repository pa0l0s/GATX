# 2. Dynamic Model

The **Dynamic Model** describes system *behaviour* — how objects collaborate and how
state changes over time. UML offers several behavioural diagrams; this document uses the
three most informative for GATX:

- **Sequence diagrams** — the message exchange that realises a use case.
- **State machine diagram** — the lifecycle of the key domain object (`AssemblyLine`).
- **Activity diagram** — the end-to-end planner workflow.

## 2.1 Sequence — Log In (UC-01)

Shows how a request travels through the Clean Architecture layers. Note the **MediatR
pipeline**: the request is *sent*, a validation behaviour runs first, then the handler.

```mermaid
sequenceDiagram
    actor Planner
    participant SPA as React SPA
    participant Nginx as nginx (reverse proxy)
    participant Ctrl as AuthController
    participant Med as MediatR (ISender)
    participant Val as ValidationBehavior
    participant H as LoginCommandHandler
    participant Hash as IPasswordHasher
    participant Jwt as IJwtTokenGenerator
    participant DB as PostgreSQL

    Planner->>SPA: enter username + password
    SPA->>Nginx: POST /api/auth/login
    Nginx->>Ctrl: forward request
    Ctrl->>Med: Send(LoginCommand)
    Med->>Val: handle(LoginCommand)
    Val->>Val: validate (username/password not empty)
    alt invalid
        Val-->>Ctrl: ValidationException → 400
    else valid
        Val->>H: next()
        H->>DB: SELECT user WHERE username = ?
        DB-->>H: User (or null)
        alt user null or bad password
            H->>Hash: Verify(password, hash)
            Hash-->>H: false
            H-->>Ctrl: UnauthorizedAccessException → 401
        else credentials ok
            H->>Hash: Verify(password, hash)
            Hash-->>H: true
            H->>Jwt: GenerateToken(userId, username)
            Jwt-->>H: signed JWT
            H-->>Ctrl: AuthResultDto(token, username)
            Ctrl-->>SPA: 200 { token, username }
            SPA->>SPA: store token for later requests
        end
    end
```

## 2.2 Sequence — Allocate Workstation to Line (UC-09)

Illustrates a write path with multiple invariant checks before persistence, and the
bearer-token authorisation gate that every business request passes through.

```mermaid
sequenceDiagram
    actor Planner
    participant SPA as React SPA
    participant Nginx as nginx
    participant Auth as JWT middleware
    participant Ctrl as AssemblyLinesController
    participant Med as MediatR
    participant H as AllocateWorkstationCommandHandler
    participant Ord as AllocationOrdering
    participant DB as PostgreSQL

    Planner->>SPA: choose line + workstation → "Add"
    SPA->>Nginx: POST /api/assembly-lines/{id}/workstations<br/>(Authorization: Bearer token)
    Nginx->>Auth: forward
    Auth->>Auth: validate JWT (issuer/audience/lifetime/signature)
    alt token invalid/absent
        Auth-->>SPA: 401 Unauthorized
    else token valid
        Auth->>Ctrl: Allocate(id, workstationId)
        Ctrl->>Med: Send(AllocateWorkstationCommand)
        Med->>H: handle()
        H->>Ord: EnsureLineExistsAsync(id)
        Ord->>DB: SELECT 1 FROM assembly_lines WHERE id = ?
        DB-->>Ord: exists / not found
        H->>DB: SELECT EXISTS(workstation id = ?)
        H->>DB: SELECT EXISTS(allocation line+workstation)
        alt not found / duplicate
            H-->>Ctrl: 404 / 409
        else ok
            H->>DB: SELECT MAX(position) WHERE line = ?
            DB-->>H: maxPosition
            H->>DB: INSERT AssemblyLineWorkstation(position = max+1)
            H->>DB: SaveChanges()
            H->>Ord: LoadAsync(id) → ordered allocations
            Ord->>DB: SELECT ... ORDER BY position
            DB-->>Ord: rows
            Ord-->>H: IReadOnlyList<AllocationDto>
            H-->>Ctrl: allocations
            Ctrl-->>SPA: 201 Created (ordered list)
        end
    end
```

## 2.3 State machine — AssemblyLine lifecycle

An `AssemblyLine` is a stateful domain object. Its `Active` flag and its collection of
allocations define the states below. Guards/effects reference the domain methods
(`SetActive`, `Rename`, `MoveToProduct`, allocation commands).

```mermaid
stateDiagram-v2
    [*] --> Inactive: create(productId, name, active=false)
    [*] --> Active: create(productId, name, active=true)

    Inactive --> Active: SetActive(true)
    Active --> Inactive: SetActive(false)

    state "Configuring route" as Configuring {
        [*] --> Empty
        Empty --> HasWorkstations: Allocate first workstation
        HasWorkstations --> HasWorkstations: Allocate / Reorder / Remove
        HasWorkstations --> Empty: Remove last workstation
    }

    Inactive --> Configuring: edit allocations
    Active --> Configuring: edit allocations
    Configuring --> Inactive: done (inactive)
    Configuring --> Active: done (active)

    Inactive --> [*]: Delete line
    Active --> [*]: Delete line

    note right of Active
        Active lines are the ones a shop
        floor would actually run.
        Rename / MoveToProduct allowed
        in any non-deleted state.
    end note
```

## 2.4 Activity — Configure an assembly line (end-to-end)

The typical planner workflow, spanning several use cases, expressed as a UML activity
diagram with a decision node and a loop.

```mermaid
flowchart TD
    start([Start]) --> login[Log in]
    login --> hasProduct{Product<br/>exists?}
    hasProduct -- No --> createProduct[Create product]
    createProduct --> hasWs
    hasProduct -- Yes --> hasWs{Needed<br/>workstations<br/>exist?}
    hasWs -- No --> createWs[Create workstation]
    createWs --> hasWs
    hasWs -- Yes --> createLine[Create assembly line for product]
    createLine --> allocate[Allocate a workstation to the line]
    allocate --> more{Add another<br/>workstation?}
    more -- Yes --> allocate
    more -- No --> reorder{Reorder<br/>needed?}
    reorder -- Yes --> doReorder[Reorder workstations by position]
    doReorder --> activate
    reorder -- No --> activate[Activate the line]
    activate --> done([Line ready for production])
```

Together these diagrams cover the two runtime "shapes" of GATX — a **read/authenticate**
path and a **validated write** path — plus the lifecycle of its central entity. The
static structure those messages operate on is defined in the
[Logical Model](03-logical-model.md).
