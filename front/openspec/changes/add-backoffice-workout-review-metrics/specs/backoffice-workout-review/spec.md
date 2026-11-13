# Spec Delta

## MODIFIED Requirements

### Requirement: The review has three tabs

The review SHALL show the tabs "Ejercicios", "Resumen y métricas" and "Feedback", with "Ejercicios" selected on
opening. "Resumen y métricas" SHALL show the session metrics defined by `backoffice-workout-review-metrics`. "Feedback"
SHALL show an empty panel. The tab row SHALL NOT offer "Expandir todo" or a menu.

#### Scenario: Default tab

- **WHEN** the review opens
- **THEN** "Ejercicios" is selected and the exercise cards are shown
- **AND** no "Expandir todo" button is shown

#### Scenario: Metrics tab

- **WHEN** the user selects "Resumen y métricas"
- **THEN** the exercise cards are not shown and the session metrics are shown

#### Scenario: Empty tabs

- **WHEN** the user selects "Feedback"
- **THEN** the exercise cards are not shown and the panel is empty
