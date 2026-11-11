import SectionViews from "@/analytics/section-views";
import Ai from "@/sections/ai";
import AthleteExperience from "@/sections/athlete-experience";
import Faq from "@/sections/faq";
import Features from "@/sections/features";
import FinalCta from "@/sections/final-cta";
import Footer from "@/sections/footer";
import FoundingTrainers from "@/sections/founding-trainers";
import Header from "@/sections/header";
import Hero from "@/sections/hero";
import HowItWorks from "@/sections/how-it-works";
import Problem from "@/sections/problem";

/** The landing, its sections in the specification's order. */
export default function LandingPage() {
  return (
    <>
      <Header />
      <main>
        <Hero />
        <Problem />
        <Features />
        <HowItWorks />
        <AthleteExperience />
        <Ai />
        <FoundingTrainers />
        <Faq />
        <FinalCta />
      </main>
      <Footer />
      <SectionViews />
    </>
  );
}
