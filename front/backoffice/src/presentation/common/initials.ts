/**
 * First letter of the name and of the surname: "Ana" "García" → "AG". A person with neither gets the first letter of
 * their email.
 */
export function initialsOf(person: { name: string; surname: string; email: string }): string {
  const initials = [person.name, person.surname]
    .map((word) => word.trim().charAt(0).toUpperCase())
    .join("");

  return initials === "" ? person.email.trim().charAt(0).toUpperCase() : initials;
}
