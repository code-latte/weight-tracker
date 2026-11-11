# website-landing Specification

## Purpose

Trainera's public website: a one-page Spanish landing that explains the product to personal trainers and leads them
to the early-access form, plus its legal pages, with honest feature status, SEO and accessible, responsive layout.

## Requirements

### Requirement: The landing is one page with its sections in order

The website SHALL serve at `/` a single page with, in this order: header, hero, problem, features, how it works,
athlete experience, artificial intelligence, Founding Trainers, FAQ, final call to action and footer. Their texts
SHALL be the ones of the landing specification appendix, in Spanish.

#### Scenario: Sections in order

- **WHEN** a visitor opens `/`
- **THEN** the page shows the hero heading "Menos gestión. Más entrenamiento." as its only level-1 heading
- **AND** the sections follow in the order above, each under its own level-2 heading

### Requirement: The header navigates to the sections

The header SHALL stay at the top while scrolling and become more compact once the page is scrolled. It SHALL show the
Trainera logo, the links "Funcionalidades", "Cómo funciona", "Para entrenadores" and "FAQ", and an "Acceso anticipado"
button in `#F97316` with `#0F172A` text. Each link SHALL scroll to its section, smoothly unless the visitor prefers
reduced motion.

#### Scenario: Link to a section

- **WHEN** the visitor clicks "FAQ" in the header
- **THEN** the page scrolls to the FAQ section and its address ends in `#faq`

#### Scenario: Compact on scroll

- **WHEN** the visitor scrolls down from the top of the page
- **THEN** the header stays visible and becomes more compact

#### Scenario: Reduced motion

- **WHEN** the visitor's system prefers reduced motion and they follow a link
- **THEN** the page jumps to the section without a smooth scroll

### Requirement: A mobile menu replaces the links on small screens

Below 900 px the header SHALL show the logo, the "Acceso anticipado" button and a menu button labelled "Abrir menú"
instead of the links. The menu SHALL open with the links, close when a link in it is followed or when Escape is
pressed, and be usable with the keyboard.

#### Scenario: Open and follow a link

- **WHEN** a visitor on a 375 px wide screen opens the menu and chooses "FAQ"
- **THEN** the menu closes and the page scrolls to the FAQ section

#### Scenario: Close with Escape

- **WHEN** the menu is open and the visitor presses Escape
- **THEN** the menu closes and focus returns to the menu button

### Requirement: Every call to action leads to the early-access form

The header button, the hero's main call to action, the Founding Trainers call to action and the final call to action
SHALL lead to the early-access form (`#acceso-anticipado`). The hero's secondary call to action, "Descubrir Trainera",
SHALL lead to the features section.

#### Scenario: Hero call to action

- **WHEN** the visitor clicks "Solicitar acceso anticipado" in the hero
- **THEN** the page scrolls to the early-access form

### Requirement: Features show whether they are available

Every feature, artificial-intelligence capability and step SHALL be shown either as available or with a
"Próximamente" label, as the website's content declares. No capability that the product lacks SHALL be shown as
available. Every artificial-intelligence capability, training feedback, progress charts and statistics, the rest timer
and the calendar SHALL show "Próximamente".

#### Scenario: AI capabilities

- **WHEN** the visitor reads the artificial-intelligence section
- **THEN** every listed capability carries the "Próximamente" label
- **AND** the section shows "La IA propone. El entrenador decide."

#### Scenario: Available feature

- **WHEN** the visitor reads the workout planning feature
- **THEN** it carries no "Próximamente" label

### Requirement: Product images are demonstrative

The hero, the features and the steps SHALL show images of the Trainera backoffice and athlete app made with fictitious
data. Every product image SHALL say in its alternative text and caption that it is a demonstrative image, and no
product image SHALL show real user data. No stock photograph of people training SHALL be the main image of any
section.

#### Scenario: Hero image

- **WHEN** the visitor views the hero
- **THEN** its product image has an alternative text that includes "Imagen demostrativa"

### Requirement: The Founding Trainers programme states its limit

The Founding Trainers section SHALL show the "PROGRAMA FOUNDING TRAINERS" badge, the appendix's heading, description
and benefits, and that the programme is limited to ten trainers. It SHALL NOT show a countdown or a number of places
left.

#### Scenario: Limit shown as a condition

- **WHEN** the visitor reads the Founding Trainers section
- **THEN** it says the programme is limited to 10 trainers
- **AND** no counter of remaining places is shown

### Requirement: The FAQ answers open one at a time on demand

The FAQ SHALL list the appendix's questions, each a button that shows or hides its answer and reports whether it is
expanded. The pricing answer SHALL say there will be plans adapted to each trainer and SHALL NOT show any price.

#### Scenario: Open an answer

- **WHEN** the visitor activates "¿Qué es Trainera?" with the keyboard
- **THEN** its answer is shown and the question reports it is expanded

#### Scenario: No price

- **WHEN** the visitor opens "¿Cuánto cuesta Trainera?"
- **THEN** the answer mentions plans and shows no amount

### Requirement: The footer shows only what exists

The footer SHALL show the logo, "La plataforma para entrenadores personales.", the links "Funcionalidades", "Cómo
funciona", "Acceso anticipado" and "Contacto" (which leads to the early-access form), the four legal pages and "© 2026
Trainera. Todos los derechos reservados." It SHALL NOT show social profiles or a contact address.

#### Scenario: Footer links

- **WHEN** the visitor reads the footer
- **THEN** it links to "Aviso legal", "Política de privacidad", "Política de cookies" and "Condiciones de uso"
- **AND** it shows no Instagram, LinkedIn or TikTok link and no email address

### Requirement: The legal pages exist

The website SHALL serve `/aviso-legal`, `/privacidad`, `/cookies` and `/condiciones`, each with its title, the
header logo linking back to `/`, and its text. Until the product owner supplies a text, its page SHALL show a clearly
marked "Contenido pendiente" notice instead of invented content.

#### Scenario: Pending legal text

- **WHEN** the visitor opens `/privacidad` before its text is supplied
- **THEN** the page shows "Política de privacidad" and the "Contenido pendiente" notice

### Requirement: The launch state is one setting

The website SHALL read its launch state from one setting whose only supported value in this version is `prelaunch`,
the default. The labels of every call to action and the blocks shown SHALL come from the content for that state, not
from the components.

#### Scenario: Pre-launch labels

- **WHEN** the launch state is not set
- **THEN** the main calls to action read "Solicitar acceso anticipado" and no login, signup or pricing link is shown

### Requirement: The landing is found and shared correctly

The page SHALL have the title "Trainera | Plataforma de gestión para entrenadores personales", the appendix's meta
description, Open Graph and Twitter card data with a Trainera image, the Trainera favicons and a canonical URL from
the site's base address. The website SHALL serve `/sitemap.xml` listing its pages and `/robots.txt` allowing them.

#### Scenario: Metadata

- **WHEN** a crawler reads `/`
- **THEN** it finds that title, that description and an `og:image`

#### Scenario: Sitemap

- **WHEN** a crawler reads `/sitemap.xml`
- **THEN** it lists `/` and the four legal pages

### Requirement: The landing works on every screen and with the keyboard

The page SHALL have no horizontal scroll from 360 px wide, stack its cards and columns in one column below 600 px, show
the hero text before its image on small screens, keep touch targets at least 44 px high, show a visible focus on every
interactive element and meet WCAG AA contrast. Images below the first screen SHALL load lazily.

#### Scenario: Narrow screen

- **WHEN** the page is shown 360 px wide
- **THEN** nothing overflows horizontally and the problem cards are in one column

#### Scenario: Keyboard

- **WHEN** a visitor moves through the page with Tab
- **THEN** every link, button and field shows a visible focus in reading order

### Requirement: Visits and calls to action are tracked as named events

The website SHALL emit a named event for every call-to-action click (with its location), the first interaction with
the form, every submission and its outcome, and each section reaching the screen, carrying the UTM parameters and the
referrer read on arrival. In this version the events SHALL go nowhere and the website SHALL set no cookie.

#### Scenario: CTA click

- **WHEN** the visitor arrives with `?utm_source=instagram` and clicks the hero's main call to action
- **THEN** a call-to-action event for the hero is emitted with `utm_source` `instagram`
- **AND** no cookie is set
