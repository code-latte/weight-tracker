import Badge from "@/components/badge";
import CtaLink from "@/components/cta-link";
import ProductImage from "@/components/product-image";
import { buttonClass } from "@/components/button";
import { config } from "@/config";
import { ctas, formAnchor, hero } from "@/content/landing";
import styles from "./hero.module.css";

/** The first screen: what Trainera is, the main call to action, and the product with a phone over it. */
export default function Hero() {
  const [first, second] = hero.title.split(". ");

  return (
    <section
      id="inicio"
      data-section="inicio"
      aria-labelledby="inicio-titulo"
      className={styles.hero}
    >
      <div className={styles.grid}>
        <div className={styles.copy}>
          <Badge>{hero.overline}</Badge>
          <h1 id="inicio-titulo" className={styles.title}>
            <span className={styles.line}>{first}.</span>{" "}
            <span className={`${styles.line} ${styles.accent}`}>{second}</span>
          </h1>
          <p className={styles.description}>{hero.description}</p>
          <div className={styles.actions}>
            <CtaLink href={formAnchor} location="hero" size="large">
              {ctas[config.launchState].primary}
            </CtaLink>
            <a
              href={hero.secondaryCta.href}
              className={buttonClass({ variant: "outline", size: "large" })}
            >
              {hero.secondaryCta.label}
            </a>
          </div>
          <p className={styles.support}>{hero.support}</p>
        </div>
        <div className={styles.visual}>
          <ProductImage image={hero.desktopImage} priority />
          <div className={styles.phone}>
            <ProductImage image={hero.phoneImage} device="phone" priority caption={false} />
          </div>
        </div>
      </div>
    </section>
  );
}
