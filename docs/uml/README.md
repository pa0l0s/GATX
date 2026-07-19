# GATX — UML Model Documentation

This folder documents the **GATX Assembly Manager** system using the UML modelling
views described in the Sparx Systems UML tutorial
([The UML Structure](https://sparxsystems.com/resources/tutorials/uml/part1.html)).
Each view answers a different question about the system:

| View | UML question | File |
|------|--------------|------|
| **Use Case Model** | *What* does the system do, and for *whom*? | [01-use-case-model.md](01-use-case-model.md) |
| **Dynamic Model** | *How* does the system behave over time? | [02-dynamic-model.md](02-dynamic-model.md) |
| **Logical Model** | *What* are the things in the system and how do they relate? | [03-logical-model.md](03-logical-model.md) |
| **Component Model** | *How* is the software organised into deployable parts? | [04-component-model.md](04-component-model.md) |
| **Physical Model** | *Where* does the software actually run? | [05-physical-model.md](05-physical-model.md) |

> All diagrams are written in **Mermaid** and render directly on GitHub. Where UML has
> no native Mermaid shape (use-case ovals, deployment nodes), the diagram uses the
> closest Mermaid construct and the accompanying text names the true UML element.

---

## System overview

**GATX Assembly Manager** is a web application for configuring manufacturing
**assembly lines**. A production planner defines the **products** being built, the
**workstations** available on the shop floor, and the **assembly lines** that route a
product through an *ordered* sequence of workstations.

### Ubiquitous language

| Term | Meaning |
|------|---------|
| **Product** | An item that is manufactured (e.g. a pump, a valve). |
| **Workstation** | A physical station on the shop floor (short name, full name, PC name). |
| **Assembly Line** | A named, ordered route that builds one product through several workstations. May be *active* or *inactive*. |
| **Allocation** | The placement of a workstation at a specific **position** on an assembly line (the `AssemblyLineWorkstation` join). |
| **User** | An authenticated operator of the system (planner / production engineer). |

### Technology at a glance

- **Backend** — .NET 8, Clean Architecture (WebApi → Application → Domain, with an
  Infrastructure adapter layer), CQRS via **MediatR**, validation via **FluentValidation**,
  persistence via **EF Core** on **PostgreSQL**, stateless auth via **JWT**.
- **Frontend** — React + Vite single-page application (`assembly-manager`), served by
  **nginx** which also reverse-proxies `/api`.
- **Infrastructure** — AWS (EC2 + RDS PostgreSQL + ECR), provisioned with **Terraform**
  and deployed through **GitHub Actions** using OIDC federation.

The five views that follow describe this system from the outside in.
