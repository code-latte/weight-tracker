import type { LegalSection } from "./legal";

/** The privacy policy, as supplied by the product owner. */
export const privacyPolicy: LegalSection[] = [
  {
    id: "responsable",
    heading: "1. Responsable del tratamiento",
    blocks: [
      {
        paragraph:
          "En cumplimiento del Reglamento (UE) 2016/679, General de Protección de Datos (RGPD), y de la Ley Orgánica 3/2018, de Protección de Datos Personales y garantía de los derechos digitales (LOPDGDD), se informa a los usuarios de que el responsable del tratamiento de sus datos personales es:",
      },
      {
        details: [
          { term: "Responsable", text: "Víctor Lendinez Agudo" },
          { term: "Nombre comercial", text: "Trainera" },
          { term: "Sitio web", text: "https://trainera.es" },
          { term: "Correo electrónico de contacto", text: "privacidad@trainera.es" },
          { term: "País de establecimiento", text: "España" },
        ],
      },
      {
        paragraph:
          "Para cualquier consulta relacionada con el tratamiento de datos personales, puedes contactar con el responsable mediante la dirección de correo electrónico indicada.",
      },
    ],
  },
  {
    id: "datos-recopilados",
    heading: "2. Datos personales que recopilamos",
    blocks: [
      {
        paragraph:
          "A través del formulario de inscripción disponible en trainera.es, recopilamos únicamente la siguiente información facilitada voluntariamente por el usuario:",
      },
      { list: [{ text: "Dirección de correo electrónico." }] },
      {
        paragraph:
          "Esta información se utiliza para gestionar el interés manifestado por el usuario en Trainera y permitir el envío de las comunicaciones solicitadas.",
      },
      {
        paragraph:
          "Actualmente, el formulario no está destinado a recopilar datos relativos a la salud, información sobre entrenamientos ni datos personales de atletas.",
      },
    ],
  },
  {
    id: "finalidades",
    heading: "3. Finalidades del tratamiento",
    blocks: [
      {
        paragraph:
          "Los datos personales facilitados se utilizarán para las siguientes finalidades:",
      },
      {
        list: [
          { text: "Informar sobre el lanzamiento y la disponibilidad de Trainera." },
          {
            text: "Comunicar novedades, funcionalidades y actualizaciones relevantes del producto.",
          },
          {
            text: "Enviar información comercial o promocional relacionada con Trainera, conforme al consentimiento otorgado por el usuario.",
          },
          {
            text: "Atender solicitudes relacionadas con la inscripción y gestionar la baja de las comunicaciones.",
          },
        ],
      },
      {
        paragraph:
          "Los datos no se utilizarán para finalidades incompatibles con las indicadas ni se venderán a terceros.",
      },
    ],
  },
  {
    id: "base-juridica",
    heading: "4. Base jurídica del tratamiento",
    blocks: [
      {
        paragraph:
          "La base jurídica para el tratamiento de los datos personales es el **consentimiento del interesado**, conforme al artículo 6.1.a del Reglamento General de Protección de Datos.",
      },
      {
        paragraph:
          "Este consentimiento se obtiene mediante una acción afirmativa del usuario al completar el formulario y aceptar recibir comunicaciones sobre Trainera.",
      },
      {
        paragraph:
          "El consentimiento es voluntario y puede retirarse en cualquier momento, sin que ello afecte a la licitud del tratamiento efectuado con anterioridad.",
      },
    ],
  },
  {
    id: "conservacion",
    heading: "5. Conservación de los datos",
    blocks: [
      {
        paragraph:
          "Los datos personales se conservarán mientras el usuario mantenga su consentimiento para recibir comunicaciones sobre Trainera y las finalidades indicadas continúen vigentes.",
      },
      {
        paragraph:
          "El usuario podrá solicitar en cualquier momento la eliminación de su dirección de correo electrónico.",
      },
      {
        paragraph:
          "Cuando los datos dejen de ser necesarios para las finalidades previstas o se retire el consentimiento, serán suprimidos, sin perjuicio de la conservación de la información mínima que resulte necesaria para acreditar el cumplimiento de obligaciones legales o atender posibles responsabilidades.",
      },
    ],
  },
  {
    id: "destinatarios",
    heading: "6. Destinatarios y alojamiento de los datos",
    blocks: [
      {
        paragraph:
          "Los datos personales se almacenan en una base de datos alojada en infraestructura contratada con **IONOS**, en servidores ubicados en España.",
      },
      {
        paragraph:
          "IONOS actúa como proveedor de infraestructura y, en lo que corresponda, como encargado del tratamiento de los datos personales por cuenta del responsable.",
      },
      {
        paragraph:
          "No se comercializan ni se ceden los datos a terceros con fines publicitarios propios.",
      },
      {
        paragraph:
          "Podrán comunicarse datos a las autoridades públicas cuando exista una obligación legal.",
      },
      {
        paragraph:
          "En el tratamiento descrito, Trainera no tiene prevista la realización de transferencias internacionales de datos fuera del Espacio Económico Europeo. Esta circunstancia se revisará si se incorporan proveedores o servicios que impliquen dichas transferencias.",
      },
    ],
  },
  {
    id: "derechos",
    heading: "7. Derechos de los usuarios",
    blocks: [
      {
        paragraph:
          "El usuario podrá ejercer los derechos reconocidos por la normativa de protección de datos, en particular:",
      },
      {
        list: [
          { term: "Acceso", text: "conocer qué datos personales se están tratando." },
          { term: "Rectificación", text: "solicitar la corrección de datos inexactos." },
          { term: "Supresión", text: "solicitar la eliminación de sus datos cuando corresponda." },
          {
            term: "Oposición",
            text: "oponerse al tratamiento en los supuestos legalmente previstos.",
          },
          {
            term: "Limitación",
            text: "solicitar la limitación del tratamiento en determinadas circunstancias.",
          },
          {
            term: "Portabilidad",
            text: "recibir los datos facilitados en un formato estructurado, de uso común y lectura mecánica, cuando proceda.",
          },
          {
            term: "Retirada del consentimiento",
            text: "dejar de recibir comunicaciones en cualquier momento.",
          },
        ],
      },
      { paragraph: "Para ejercer estos derechos, el usuario podrá escribir a:" },
      { highlight: "privacidad@trainera.es" },
      {
        paragraph:
          "El responsable atenderá las solicitudes dentro de los plazos legalmente establecidos y podrá solicitar información adicional cuando resulte necesaria para verificar la identidad del solicitante.",
      },
      {
        paragraph:
          "Asimismo, el usuario tiene derecho a presentar una reclamación ante la Agencia Española de Protección de Datos (AEPD), a través de https://www.aepd.es, si considera que sus derechos no han sido respetados.",
      },
    ],
  },
  {
    id: "comunicaciones-comerciales",
    heading: "8. Comunicaciones comerciales y cancelación de la suscripción",
    blocks: [
      {
        paragraph:
          "Trainera enviará comunicaciones relacionadas con su lanzamiento, novedades, servicios y promociones únicamente a aquellas personas que hayan autorizado su recepción o cuando exista otra habilitación legal aplicable.",
      },
      {
        paragraph:
          "Los usuarios podrán retirar su consentimiento gratuitamente en cualquier momento, mediante los mecanismos de baja incluidos en las comunicaciones o enviando una solicitud a privacidad@trainera.es.",
      },
      {
        paragraph:
          "Tras recibir una solicitud válida de baja, Trainera dejará de utilizar la dirección de correo electrónico para el envío de comunicaciones comerciales.",
      },
    ],
  },
  {
    id: "seguridad",
    heading: "9. Seguridad de los datos",
    blocks: [
      {
        paragraph:
          "Trainera aplica medidas técnicas y organizativas apropiadas para proteger los datos personales frente a accesos no autorizados, pérdida, alteración o divulgación indebida.",
      },
      {
        paragraph:
          "Las medidas se seleccionan teniendo en cuenta la naturaleza de los datos tratados y los riesgos asociados al tratamiento.",
      },
      {
        paragraph:
          "El acceso a los datos personales se limita a las personas autorizadas que necesiten utilizarlos para las finalidades descritas.",
      },
    ],
  },
  {
    id: "cookies",
    heading: "10. Cookies y tecnologías similares",
    blocks: [
      {
        paragraph:
          "El sitio web puede utilizar cookies o tecnologías similares necesarias para su funcionamiento y, en su caso, para gestionar las preferencias de privacidad de los visitantes.",
      },
      {
        paragraph:
          "En caso de incorporarse tecnologías de análisis, publicidad u otras cookies sujetas a consentimiento, se informará previamente al usuario y se solicitará su autorización cuando resulte legalmente exigible.",
      },
      {
        paragraph:
          "La información detallada sobre las cookies efectivamente utilizadas, sus finalidades, proveedores y plazos de duración se facilitará mediante la correspondiente Política de Cookies.",
      },
    ],
  },
  {
    id: "caracter-voluntario",
    heading: "11. Carácter voluntario de los datos",
    blocks: [
      { paragraph: "La comunicación de la dirección de correo electrónico es voluntaria." },
      {
        paragraph:
          "No obstante, facilitarla resulta necesario para poder inscribirse en la lista de interesados y recibir información sobre Trainera.",
      },
      {
        paragraph:
          "La negativa a proporcionar el correo electrónico únicamente impedirá gestionar dicha inscripción y enviar las comunicaciones solicitadas.",
      },
    ],
  },
  {
    id: "decisiones-automatizadas",
    heading: "12. Decisiones automatizadas",
    blocks: [
      {
        paragraph:
          "Actualmente, Trainera no utiliza los datos facilitados mediante el formulario de interesados para adoptar decisiones basadas únicamente en tratamientos automatizados que produzcan efectos jurídicos sobre los usuarios o les afecten significativamente de modo similar.",
      },
      {
        paragraph:
          "Tampoco se elaboran perfiles personales de los interesados a partir de la información recogida mediante este formulario.",
      },
    ],
  },
  {
    id: "modificaciones",
    heading: "13. Modificaciones de la Política de Privacidad",
    blocks: [
      {
        paragraph:
          "Trainera podrá actualizar esta Política de Privacidad para adaptarla a cambios normativos, técnicos o en los tratamientos de datos realizados.",
      },
      {
        paragraph:
          "La versión vigente estará disponible en el sitio web, indicando su fecha de última actualización.",
      },
      {
        paragraph:
          "Cuando una modificación requiera obtener un nuevo consentimiento, este se solicitará antes de realizar el tratamiento correspondiente.",
      },
    ],
  },
  {
    id: "contacto",
    heading: "14. Contacto",
    blocks: [
      {
        paragraph:
          "Para cualquier duda, consulta o solicitud relacionada con esta Política de Privacidad o con el tratamiento de datos personales, puedes dirigirte a:",
      },
      {
        details: [
          { term: "Responsable", text: "Víctor Lendinez Agudo — Trainera" },
          { term: "Correo electrónico", text: "privacidad@trainera.es" },
          { term: "Sitio web", text: "https://trainera.es" },
        ],
      },
    ],
  },
];
