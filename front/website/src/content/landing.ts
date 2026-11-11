import type { LaunchState } from "@/config";

/**
 * Every text of the landing, in Spanish, from the product owner's specification
 * (`.stpr/plan/openspec/proposal-change-add-website-landing.landing-spec.md`). Components hold no copy: they receive it
 * from here. A capability the product does not have yet is `soon`, which the page labels "Próximamente".
 */

export type Status = "available" | "soon";

/** The icons the content names; `components/icon.tsx` maps each to its drawing. */
export type IconName =
  | "files"
  | "message"
  | "trending"
  | "clock"
  | "users"
  | "clipboard"
  | "chart"
  | "feedback"
  | "smartphone"
  | "sparkles"
  | "dumbbell"
  | "check";

/** The section ids the anchors and the analytics events use. */
export type SectionId =
  | "inicio"
  | "problema"
  | "funcionalidades"
  | "como-funciona"
  | "para-entrenadores"
  | "ia"
  | "acceso-anticipado"
  | "faq"
  | "empieza";

/** Where a call to action sits, for the analytics events. */
export type CtaLocation = "header" | "hero" | "founding" | "final" | "footer";

export interface Item {
  text: string;
  status: Status;
}

export interface ProductImage {
  src: string;
  /** Without the "Imagen demostrativa" prefix, which the image frame adds. */
  alt: string;
  width: number;
  height: number;
}

export interface Feature {
  id: string;
  icon: IconName;
  title: string;
  description: string;
  items: Item[];
  image: ProductImage;
}

export const soonLabel = "Próximamente";
export const demoImageLabel = "Imagen demostrativa";

export const nav = {
  homeLabel: "Trainera, ir al inicio",
  links: [
    { label: "Funcionalidades", href: "#funcionalidades" },
    { label: "Cómo funciona", href: "#como-funciona" },
    { label: "Para entrenadores", href: "#para-entrenadores" },
    { label: "FAQ", href: "#faq" },
  ],
  menuOpenLabel: "Abrir menú",
  menuCloseLabel: "Cerrar menú",
  menuLabel: "Menú principal",
};

/** The calls to action of each launch state; only the pre-launch state is built. */
export const ctas: Record<LaunchState, { header: string; primary: string; final: string }> = {
  prelaunch: {
    header: "Acceso anticipado",
    primary: "Solicitar acceso anticipado",
    final: "Solicitar acceso anticipado",
  },
};

export const formAnchor = "#acceso-anticipado";

export const hero = {
  overline: "LA PLATAFORMA PARA ENTRENADORES PERSONALES",
  title: "Menos gestión. Más entrenamiento.",
  description:
    "Planifica entrenamientos, gestiona deportistas y analiza su progreso desde una única plataforma. Dedica menos tiempo a organizarlo todo y más tiempo a conseguir resultados.",
  secondaryCta: { label: "Descubrir Trainera", href: "#funcionalidades" },
  support: "Estamos construyendo una nueva forma de trabajar para entrenadores personales.",
  desktopImage: {
    src: "/screens/backoffice-workout",
    alt: "editor de entrenamientos de Trainera con el catálogo de ejercicios y las series de cada ejercicio",
    width: 1440,
    height: 900,
  } satisfies ProductImage,
  phoneImage: {
    src: "/screens/app-workout",
    alt: "aplicación del deportista con el entrenamiento del día y sus ejercicios",
    width: 390,
    height: 844,
  } satisfies ProductImage,
};

export const problem = {
  title: "Entrenar personas ya es suficientemente complicado. Gestionarlo todo no debería serlo.",
  description:
    "Cuando empiezas a gestionar varios deportistas, organizar entrenamientos, registrar progresiones y mantener el contacto con cada uno puede convertirse en un trabajo a tiempo completo.",
  cards: [
    {
      icon: "files",
      title: "PDFs y documentos dispersos",
      text: "Entrenamientos repartidos entre documentos, versiones y archivos que terminan siendo difíciles de mantener.",
    },
    {
      icon: "message",
      title: "Seguimiento manual",
      text: "Resultados, pesos y observaciones enviados por WhatsApp que después tienes que revisar y organizar.",
    },
    {
      icon: "trending",
      title: "Progresiones difíciles de controlar",
      text: "Cuesta tener una visión clara de cómo evoluciona cada deportista y qué decisiones tomar en su planificación.",
    },
    {
      icon: "clock",
      title: "Demasiado tiempo gestionando",
      text: "Más tiempo organizando entrenamientos y menos tiempo analizando resultados, mejorando la planificación y atendiendo a tus deportistas.",
    },
  ] satisfies { icon: IconName; title: string; text: string }[],
  closing:
    "Trainera centraliza tu trabajo para que puedas centrarte en lo que realmente importa: tus deportistas.",
};

export const features = {
  title: "Todo lo que necesitas para entrenar a tus deportistas",
  blocks: [
    {
      id: "deportistas",
      icon: "users",
      title: "Todos tus deportistas bajo control.",
      description:
        "Centraliza la información de tus deportistas, sus entrenamientos y su evolución en un único espacio.",
      items: [
        { text: "Listado de deportistas", status: "available" },
        { text: "Invitaciones a nuevos deportistas", status: "available" },
        { text: "Acceso a sus entrenamientos", status: "available" },
        { text: "Perfil individual con sus datos relevantes", status: "soon" },
        { text: "Historial de actividad", status: "soon" },
      ],
      image: {
        src: "/screens/backoffice-athletes",
        alt: "listado de deportistas de un entrenador en el backoffice de Trainera",
        width: 1440,
        height: 900,
      },
    },
    {
      id: "planificacion",
      icon: "clipboard",
      title: "Planifica con estructura. Entrena con intención.",
      description:
        "Diseña entrenamientos, organiza semanas y construye mesociclos adaptados a los objetivos de cada deportista.",
      items: [
        { text: "Constructor de entrenamientos", status: "available" },
        { text: "Catálogo de ejercicios", status: "available" },
        { text: "Series, repeticiones, descansos y esfuerzo", status: "available" },
        { text: "Organización semanal", status: "available" },
        { text: "Mesociclos con varios bloques de entrenamiento", status: "available" },
        { text: "Plantillas de entrenamiento", status: "available" },
      ],
      image: {
        src: "/screens/backoffice-mesocycle",
        alt: "planificación semanal de un mesociclo en el backoffice de Trainera",
        width: 1440,
        height: 900,
      },
    },
    {
      id: "progresiones",
      icon: "chart",
      title: "Cada entrenamiento cuenta. Cada progreso también.",
      description:
        "Registra resultados, controla cargas y analiza la evolución de tus deportistas para tomar mejores decisiones.",
      items: [
        { text: "Registro de resultados", status: "available" },
        { text: "Historial de entrenamientos", status: "available" },
        { text: "Evolución de pesos", status: "soon" },
        { text: "Estadísticas", status: "soon" },
        { text: "Gráficas de progresión", status: "soon" },
      ],
      image: {
        src: "/screens/app-workouts",
        alt: "semana de entrenamientos de un deportista con su progreso en la aplicación de Trainera",
        width: 390,
        height: 844,
      },
    },
    {
      id: "feedback",
      icon: "feedback",
      title: "Conoce cómo está evolucionando cada deportista.",
      description:
        "Recibe información sobre las sesiones y el progreso de tus deportistas para adaptar el trabajo cuando sea necesario.",
      items: [
        { text: "Feedback de entrenamientos", status: "soon" },
        { text: "Observaciones", status: "soon" },
        { text: "Valoraciones del deportista", status: "soon" },
        { text: "Seguimiento semanal", status: "soon" },
      ],
      image: {
        src: "/screens/backoffice-workout",
        alt: "entrenamiento de un deportista con sus ejercicios en el backoffice de Trainera",
        width: 1440,
        height: 900,
      },
    },
  ] satisfies Feature[],
};

/** "Experiencia del entrenador y del deportista": the trainer's three benefits beside the athlete's app. */
export const experience = {
  title: "Una experiencia profesional para ti y para tus deportistas",
  trainer: {
    title: "Para el entrenador",
    benefits: [
      {
        title: "Control",
        text: "Todos tus deportistas, entrenamientos y progresiones en un mismo lugar.",
      },
      {
        title: "Profesionalización",
        text: "Una experiencia más organizada para el entrenador y el deportista.",
      },
      {
        title: "Eficiencia",
        text: "Menos tiempo dedicado a tareas administrativas y más tiempo al trabajo real de entrenamiento.",
      },
    ],
  },
  athlete: {
    title: "Tus deportistas también necesitan una buena experiencia.",
    description:
      "Una aplicación para que cada deportista consulte sus entrenamientos, registre sus resultados y siga su evolución.",
    items: [
      { text: "Entrenamiento del día", status: "available" },
      { text: "Ejercicios y series", status: "available" },
      { text: "Registro de pesos y repeticiones", status: "available" },
      { text: "Historial", status: "available" },
      { text: "Temporizador de descanso", status: "soon" },
      { text: "Observaciones", status: "soon" },
    ] satisfies Item[],
    image: {
      src: "/screens/app-workout",
      alt: "entrenamiento del día con sus ejercicios en la aplicación del deportista",
      width: 390,
      height: 844,
    } satisfies ProductImage,
  },
};

export const howItWorks = {
  title: "De la planificación al progreso. Todo conectado.",
  steps: [
    {
      icon: "clipboard",
      title: "Organiza",
      text: "Crea tus deportistas y diseña sus entrenamientos y planificaciones.",
      status: "available",
    },
    {
      icon: "dumbbell",
      title: "Entrena",
      text: "Tus deportistas consultan sus sesiones y registran sus resultados.",
      status: "available",
    },
    {
      icon: "chart",
      title: "Analiza",
      text: "Revisa su evolución, recibe feedback y adapta la planificación.",
      status: "soon",
    },
  ] satisfies { icon: IconName; title: string; text: string; status: Status }[],
};

export const ai = {
  title: "La inteligencia artificial al servicio del entrenador.",
  description:
    "Utiliza la IA para agilizar la planificación, generar propuestas y analizar la evolución de tus deportistas. Tú mantienes el control de las decisiones.",
  capabilities: [
    { text: "Generación de propuestas de entrenamiento", status: "soon" },
    { text: "Creación de sesiones y planificaciones", status: "soon" },
    { text: "Asistencia en la construcción de mesociclos", status: "soon" },
    { text: "Adaptaciones y modificaciones de propuestas", status: "soon" },
    { text: "Análisis de progresiones", status: "soon" },
  ] satisfies Item[],
  motto: "La IA propone. El entrenador decide.",
};

export const founding = {
  badge: "PROGRAMA FOUNDING TRAINERS",
  title: "Ayúdanos a construir la herramienta que los entrenadores necesitan.",
  description:
    "Estamos seleccionando a los primeros entrenadores que participarán activamente en el desarrollo de Trainera. Queremos conocer cómo trabajas, qué problemas encuentras y qué funcionalidades necesitas realmente.",
  limit: "Plazas limitadas a 10 entrenadores.",
  benefitsTitle: "Qué incluye",
  benefits: [
    "Acceso anticipado gratuito durante el desarrollo.",
    "Posibilidad de probar funcionalidades antes del lanzamiento.",
    "Participación directa en las decisiones de producto.",
    "Canal de comunicación directo con el equipo.",
    "Condiciones especiales de suscripción después del lanzamiento.",
  ],
  development: "Trainera está en desarrollo. Las imágenes del producto son demostrativas.",
};

export const leadForm = {
  title: "Solicita tu acceso anticipado",
  emailLabel: "Email",
  emailPlaceholder: "tu@email.com",
  consentBefore: "He leído y acepto la ",
  consentLink: "política de privacidad",
  consentAfter: ".",
  submit: "Quiero participar",
  submitting: "Enviando…",
  honeypotLabel: "Empresa",
  errors: {
    email: "Introduce un email válido.",
    consent: "Debes aceptar la política de privacidad.",
    invalid: "Introduce un email válido.",
    tooMany: "Has enviado demasiadas solicitudes. Inténtalo de nuevo en unos minutos.",
    network: "No hemos podido enviar tu solicitud. Inténtalo de nuevo.",
  },
  confirmationTitle: "¡Gracias por tu interés!",
  confirmation:
    "Hemos recibido tu solicitud. Nos pondremos en contacto contigo para conocer mejor tu forma de trabajar y explicarte cómo participar en Trainera.",
};

export const faq = {
  title: "Preguntas frecuentes",
  items: [
    {
      question: "¿Qué es Trainera?",
      answer:
        "Trainera es una plataforma diseñada para que los entrenadores personales puedan gestionar deportistas, planificar entrenamientos y analizar su evolución desde un único lugar.",
    },
    {
      question: "¿Para quién está pensada?",
      answer:
        "Está orientada a entrenadores personales, preparadores físicos y profesionales que trabajan con deportistas de forma presencial, online o híbrida.",
    },
    {
      question: "¿Necesito tener muchos deportistas para utilizar Trainera?",
      answer:
        "No. La plataforma está pensada para adaptarse a entrenadores que empiezan a gestionar sus primeros deportistas y a profesionales que ya tienen una cartera consolidada.",
    },
    {
      question: "¿Trainera sustituye al entrenador?",
      answer:
        "No. Trainera es una herramienta para ayudar al profesional a organizar su trabajo y tomar mejores decisiones. El entrenador sigue siendo quien dirige y adapta el entrenamiento.",
    },
    {
      question: "¿La inteligencia artificial crea los entrenamientos?",
      answer:
        "La IA puede ayudar a generar propuestas y facilitar tareas de planificación, pero el entrenador mantiene el control y decide qué aplicar.",
    },
    {
      question: "¿Puedo participar antes del lanzamiento?",
      answer:
        "Sí. Puedes solicitar acceso anticipado y optar a participar en el programa Founding Trainers.",
    },
    {
      question: "¿Cuánto cuesta Trainera?",
      answer:
        "Habrá diferentes planes adaptados a las necesidades de cada entrenador. Publicaremos los precios y las condiciones cuando estén confirmados.",
    },
  ],
};

export const finalCta = {
  title: "Tu trabajo es entrenar. Nosotros queremos ayudarte con todo lo demás.",
  description:
    "Descubre una nueva forma de planificar, gestionar y hacer crecer tu actividad como entrenador personal.",
};

export const footer = {
  description: "La plataforma para entrenadores personales.",
  navTitle: "Navegación",
  nav: [
    { label: "Funcionalidades", href: "#funcionalidades" },
    { label: "Cómo funciona", href: "#como-funciona" },
    { label: "Acceso anticipado", href: "#acceso-anticipado" },
    // No contact address exists yet: "Contacto" leads to the form.
    { label: "Contacto", href: "#acceso-anticipado" },
  ],
  legalTitle: "Legal",
  copyright: "© 2026 Trainera. Todos los derechos reservados.",
};
