import enAthletes from "./en/athletes.json";
import enAuth from "./en/auth.json";
import enCommon from "./en/common.json";
import enErrors from "./en/errors.json";
import enInvitations from "./en/invitations.json";
import enOnboarding from "./en/onboarding.json";
import enProfile from "./en/profile.json";
import enTrainerAssignments from "./en/trainer-assignments.json";
import enUsers from "./en/users.json";
import enWorkouts from "./en/workouts.json";
import esAthletes from "./es/athletes.json";
import esAuth from "./es/auth.json";
import esCommon from "./es/common.json";
import esErrors from "./es/errors.json";
import esInvitations from "./es/invitations.json";
import esOnboarding from "./es/onboarding.json";
import esProfile from "./es/profile.json";
import esTrainerAssignments from "./es/trainer-assignments.json";
import esUsers from "./es/users.json";
import esWorkouts from "./es/workouts.json";

const resources = {
  es: {
    athletes: esAthletes,
    auth: esAuth,
    common: esCommon,
    errors: esErrors,
    invitations: esInvitations,
    onboarding: esOnboarding,
    profile: esProfile,
    trainerAssignments: esTrainerAssignments,
    users: esUsers,
    workouts: esWorkouts,
  },
  en: {
    athletes: enAthletes,
    auth: enAuth,
    common: enCommon,
    errors: enErrors,
    invitations: enInvitations,
    onboarding: enOnboarding,
    profile: enProfile,
    trainerAssignments: enTrainerAssignments,
    users: enUsers,
    workouts: enWorkouts,
  },
};

export default resources;
