# Spec Delta

## ADDED Requirements

### Requirement: Catalog cards show the English name under the name

When the language is not English, each catalog card SHALL show the exercise's English name on a line under the name,
in a softer grey than the name. It SHALL show nothing there when English is selected, when the exercise has no English
name, or when the English name is the same as the name shown. The workout's own exercise list SHALL NOT show it.

#### Scenario: English name under the Spanish one

- **WHEN** Spanish is selected and the catalog dialog lists the squat, named "Sentadilla" and "Squat"
- **THEN** its card shows "Sentadilla" and, under it and softer, "Squat"

#### Scenario: Nothing to add

- **WHEN** an exercise has only a Spanish name, or the same name in both languages
- **THEN** its card shows its name once

#### Scenario: English selected

- **WHEN** English is selected
- **THEN** each card shows the English name once, as its name

## MODIFIED Requirements

### Requirement: Catalog filters combine and are local

Typing SHALL split the text into words and keep only the exercises whose names contain every word, in any order and
anywhere in the name, ignoring case, accents and extra spaces; a word may be only part of a longer one. With English
selected, the names looked in SHALL be only the exercise's English name; with any other language, they SHALL be the
name in that language and the English name. An exercise with no English name SHALL be looked up by the name it is
shown with. A text of only spaces SHALL keep every exercise.
Selecting a pill SHALL keep only exercises of that type, body part or muscle; selecting the selected pill again SHALL
clear it. The selected section SHALL narrow the catalog first, and the text and pills SHALL apply on top of it. All
the filters SHALL apply together, and filtering SHALL NOT send any request. No match SHALL show "Ningún ejercicio
coincide con el filtro.". The section and the filters SHALL be kept when the catalog dialog is closed and opened again
on the same screen.

#### Scenario: Filter by text

- **WHEN** the user types "sentadi"
- **THEN** only exercises whose name contains "sentadi" (such as "Sentadilla") are shown

#### Scenario: Every word must appear

- **WHEN** the user types "Curl bíceps" and an exercise is named "Curl de bíceps"
- **THEN** that exercise is shown

#### Scenario: Words in any order

- **WHEN** the user types "polea tríceps" and an exercise is named "Extensión de tríceps en polea"
- **THEN** that exercise is shown, and exercises that lack one of the words are not

#### Scenario: Spanish also looks in English

- **WHEN** Spanish is selected and the user types "Squat"
- **THEN** the exercise shown as "Sentadilla" is listed

#### Scenario: English looks only in English

- **WHEN** English is selected and the user types "Sentadilla"
- **THEN** no exercise is listed for that text
- **AND** typing "Squat" lists the squat

#### Scenario: Filter by body part and text together

- **WHEN** the user selects the pill "Piernas" and types "prensa"
- **THEN** only legs exercises whose name contains "prensa" are shown

#### Scenario: Filter by type and body part together

- **WHEN** the user selects the pills "Fuerza" and "Piernas"
- **THEN** only strength exercises of the legs are shown

#### Scenario: Filter by section and body part together

- **WHEN** the user selects "Favoritos" and the pill "Piernas"
- **THEN** only favorite legs exercises are shown
- **AND** no request is sent

#### Scenario: Clear the body part

- **WHEN** the user selects the already selected pill "Piernas"
- **THEN** no pill is selected and the body part no longer filters the list

#### Scenario: Nothing matches

- **WHEN** no exercise matches the filters
- **THEN** "Ningún ejercicio coincide con el filtro." is shown

#### Scenario: Filters kept on reopening

- **WHEN** the user types "prensa", closes the catalog dialog and clicks "Añadir ejercicio" again
- **THEN** the filter field holds "prensa" and only matching exercises are shown

#### Scenario: Type kept on reopening

- **WHEN** the user selects "Cardio", closes the catalog dialog and opens it again
- **THEN** "Cardio" is still selected and only cardio exercises are shown

#### Scenario: Section kept on reopening

- **WHEN** the user selects "Mis ejercicios", closes the catalog dialog and opens it again
- **THEN** "Mis ejercicios" is still selected and only the user's own exercises are shown
