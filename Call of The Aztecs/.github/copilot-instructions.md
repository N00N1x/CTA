# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction

## Code Style
- Use specific formatting rules
- Follow naming conventions

## Project-Specific Rules
- FlameTowertrap should reset automatically after a configurable delay (resetAfterActive/resetDelay). Keep reset behavior implemented.
- The top warning particle (`warningEffect`) must play a `warningClip` using an AudioSource on the warning ParticleSystem GameObject while the warning is active; fallback to the trap AudioSource if the warning AudioSource is not available.
- Include a boolean `debugMode` field in trap scripts to control logging and avoid 'debugMode' undefined errors.
- WindTrap should use a trigger collider to detect only the Player via `CompareTag("Player")`. The particle system is visual only and plays only while active; it should apply continuous force to the player's Rigidbody in `FixedUpdate` for smoothness. Expose inspector controls: `pushForce`, `pushDuration`, `cooldown`, `direction`, `autoRepeat`, `repeatDelay`, and animator controls, including optional animator-driven duration.

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.
