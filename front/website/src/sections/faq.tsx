import Accordion from "@/components/accordion";
import Section from "@/components/section";
import { faq } from "@/content/landing";

/** The common questions, each opening on demand. */
export default function Faq() {
  return (
    <Section id="faq" tone="fog" title={faq.title} centered>
      <Accordion items={faq.items} />
    </Section>
  );
}
