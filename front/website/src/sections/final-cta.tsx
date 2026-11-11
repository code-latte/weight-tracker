import CtaLink from "@/components/cta-link";
import { config } from "@/config";
import { ctas, finalCta, formAnchor } from "@/content/landing";
import styles from "./final-cta.module.css";
import sectionStyles from "@/components/section.module.css";

/** The closing call to action, on the corporate gradient. */
export default function FinalCta() {
  return (
    <section
      id="empieza"
      data-section="empieza"
      aria-labelledby="empieza-titulo"
      className={`${sectionStyles.section} ${sectionStyles.gradient}`}
    >
      <div className={`${sectionStyles.container} ${styles.content}`}>
        <h2 id="empieza-titulo" className={styles.title}>
          {finalCta.title}
        </h2>
        <p className={styles.description}>{finalCta.description}</p>
        <CtaLink href={formAnchor} location="final" size="large">
          {ctas[config.launchState].final}
        </CtaLink>
      </div>
    </section>
  );
}
