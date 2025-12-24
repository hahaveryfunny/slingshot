# 🎯 Slingshot Game (Unity)

A **complete, playable Unity game** built around a **slingshot-based core mechanic**, progression systems, and a simple economy loop.

This project was created **before adopting an event-driven architecture**, with the explicit goal of answering one question:

> **“Can I fully finish and ship a game-sized project?”**

The answer was yes — this repository represents that milestone.

---

## 🕹️ Gameplay Overview

- Slingshot aiming and shooting mechanic
- Physics-based projectile interactions
- Multiple object / target types
- Upgrade and progression system
- Market-style upgrade UI
- Designed with mobile-style controls in mind

<p align="center">
  <img src="https://github.com/user-attachments/assets/2c3bfb0c-6e5c-44ae-b7ed-ed7cdb85bdb5" width="256">
</p>

---

## ✨ Systems Implemented

- Slingshot input & release logic
- Projectile physics and collision handling
- Basic level / encounter flow
- Upgrade & progression system
- Game state management
- UI for gameplay and upgrades
- Replayable gameplay loop

This is a **full game**, not a mechanic demo.

---

## 🧠 Technical Context

- Built using **Unity + C#**
- Core systems are coordinated primarily via **Singleton-style managers**
- This architecture was chosen intentionally at the time to:
  - Reduce complexity
  - Focus on finishing the project end-to-end
  - Avoid premature abstraction

---

## ⚖️ Design & Architecture Notes

### Singleton-Based Architecture

**Why it was used**
- Clear global access to core systems
- Faster iteration during early development
- Simpler mental model while focusing on completion

**Trade-offs**
- Tighter coupling between systems
- Harder to extend cleanly at scale
- Less flexible than event-driven approaches

After completing this project, later repositories in this profile move toward:
- Event-driven communication
- More modular system boundaries
- Reduced reliance on global state

This repository is included to show **project completion first, architectural refinement later**.

---

## 📌 What This Project Demonstrates

- Ability to **finish a complete game**, not just prototypes
- Managing multiple gameplay systems in a single project
- Understanding of progression and player feedback loops
- Conscious architectural decisions based on development goals
- Clear learning progression across projects

---

## 📈 Position in This Portfolio

This is the **earliest complete project** in this GitHub profile.

More recent projects demonstrate:
- Event-based architectures
- Cleaner decoupling between systems
- Reusable gameplay modules
- Greater focus on scalability and maintainability

This project remains important as proof of **end-to-end execution**.


