# Spec Delta

## ADDED Requirements

### Requirement: The profile title row opens Ajustes

The Perfil tab SHALL show the title "Perfil" and, on the same row at the right, a settings button with a cog icon and
the accessible name "Ajustes". Tapping it SHALL open the Ajustes screen in the Perfil tab.

#### Scenario: Title row

- **WHEN** a signed-in athlete opens the Perfil tab
- **THEN** the screen shows the heading "Perfil" and the "Ajustes" button on the same row

#### Scenario: Settings opens Ajustes

- **WHEN** the athlete taps "Ajustes"
- **THEN** the Ajustes screen is shown, with the tab bar visible, and no request is made

## REMOVED Requirements

### Requirement: The profile screen has a title row with a settings button

**Reason**: The cog is no longer inert: it opens the new Ajustes screen, so the "Settings not wired" scenario no
longer holds.

**Migration**: Replaced by "The profile title row opens Ajustes", which keeps the title row scenario and replaces the
inert-cog scenario with "Settings opens Ajustes".
