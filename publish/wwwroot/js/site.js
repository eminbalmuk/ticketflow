(() => {
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  if (!reducedMotion) {
    window.addEventListener("pageshow", () => {
      document.body.classList.add("is-page-ready");
    });
  }

  const story = document.querySelector("[data-scroll-story]");
  if (story) {
    const ticket = story.querySelector(".animated-ticket");
    const pen = story.querySelector(".story-pen");
    const writingLine = story.querySelector(".ticket-writing");
    const cloudBack = story.querySelector(".sky-layer-back");
    const cloudFront = story.querySelector(".sky-layer-front");
    const groundLine = story.querySelector(".ground-line");
    const cards = Array.from(story.querySelectorAll("[data-story-step]"));

    const setCurrentCard = (progress) => {
      const activeIndex = Math.round(progress * Math.max(0, cards.length - 1));
      cards.forEach((card, cardIndex) => {
        card.classList.toggle("is-current", cardIndex === activeIndex);
      });
    };

    if (ticket && !reducedMotion && window.gsap && window.ScrollTrigger) {
      gsap.registerPlugin(ScrollTrigger);
      setCurrentCard(0);
      gsap.set(pen, { autoAlpha: 0 });
      gsap.set(writingLine, { scaleX: 0, transformOrigin: "left center" });

      const timeline = gsap.timeline({
        defaults: { ease: "none" },
        scrollTrigger: {
          trigger: story,
          start: "top top",
          end: "bottom bottom",
          scrub: 0.7,
          invalidateOnRefresh: true,
          onUpdate: (self) => setCurrentCard(self.progress)
        }
      });

      timeline
        .set(ticket, {
          x: "6vw",
          y: "-34vh",
          rotate: -10,
          scale: 0.7,
          transformOrigin: "50% 50%"
        }, 0)
        .to(ticket, {
          x: "-8vw",
          y: "-18vh",
          rotate: 8,
          scale: 0.82,
          ease: "sine.inOut"
        }, 0)
        .to(ticket, {
          x: "7vw",
          y: "4vh",
          rotate: -6,
          scale: 0.94,
          ease: "sine.inOut"
        }, 0.22)
        .to(ticket, {
          x: "-5vw",
          y: "26vh",
          rotate: 4,
          scale: 1.06,
          ease: "sine.inOut"
        }, 0.48)
        .to(ticket, {
          x: "1vw",
          y: "39vh",
          rotate: -1,
          scale: 1.14,
          ease: "power1.out"
        }, 0.72)
        .to(ticket, {
          x: "0vw",
          y: "37vh",
          rotate: 0,
          scale: 1.12,
          ease: "sine.out"
        }, 0.88)
        .to(cloudBack, { y: -145, x: -22, ease: "none" }, 0)
        .to(cloudFront, { y: 125, x: 26, ease: "none" }, 0)
        .to(groundLine, { autoAlpha: 1, y: -12, ease: "sine.out" }, 0.62)
        .fromTo(pen, {
          autoAlpha: 0,
          x: "18vw",
          y: "47vh",
          rotate: -28,
          scale: 0.92
        }, {
          autoAlpha: 1,
          x: "8vw",
          y: "36vh",
          rotate: -16,
          scale: 0.98,
          ease: "sine.out"
        }, 0.62)
        .to(pen, {
          x: "2vw",
          y: "34vh",
          rotate: -7,
          ease: "sine.inOut"
        }, 0.78)
        .to(writingLine, { scaleX: 1, ease: "power1.out" }, 0.8);
    } else {
      setCurrentCard(0);
    }
  }

  document.addEventListener("submit", (event) => {
    const submitter = event.submitter;
    if (!(submitter instanceof HTMLButtonElement)) {
      return;
    }

    submitter.classList.add("is-submitting");
  });
})();
