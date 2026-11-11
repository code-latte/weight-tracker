import { demoImageLabel, type ProductImage as ProductImageContent } from "@/content/landing";
import styles from "./product-image.module.css";

/** The files of an image in one format: two widths for a desktop screenshot, one for a phone screen. */
function srcSetOf(image: ProductImageContent, format: "avif" | "webp"): string {
  if (image.width < image.height) {
    return `${image.src}.${format}`;
  }
  return `${image.src}-960.${format} 960w, ${image.src}.${format} ${image.width}w`;
}

interface ProductImageProps {
  image: ProductImageContent;
  /** A phone screen gets a device frame. */
  device?: "desktop" | "phone";
  /** The images of the first screen load at once; the rest when they come near. */
  priority?: boolean;
  caption?: boolean;
  onInk?: boolean;
}

/**
 * A product screenshot made with fictitious data, in AVIF and WebP. Its alternative text and caption say it is
 * demonstrative, so no visitor takes it for real data.
 */
export default function ProductImage({
  image,
  device = "desktop",
  priority = false,
  caption = true,
  onInk = false,
}: ProductImageProps) {
  // Desktop screenshots also ship 960 px wide, for phones and the narrower columns.
  const sizes = device === "desktop" ? "(min-width: 900px) 55vw, 100vw" : undefined;

  return (
    <figure className={`${styles.figure} ${onInk ? styles.onInk : ""}`}>
      <div className={`${styles.frame} ${device === "phone" ? styles.phone : ""}`}>
        <picture>
          <source srcSet={srcSetOf(image, "avif")} sizes={sizes} type="image/avif" />
          {/* A static export has no image optimiser: the files ship optimised, in the formats above. */}
          <img
            className={styles.image}
            src={`${image.src}.webp`}
            srcSet={srcSetOf(image, "webp")}
            sizes={sizes}
            alt={`${demoImageLabel}: ${image.alt}`}
            width={image.width}
            height={image.height}
            loading={priority ? "eager" : "lazy"}
            decoding="async"
          />
        </picture>
      </div>
      {caption && <figcaption className={styles.caption}>{demoImageLabel}</figcaption>}
    </figure>
  );
}
