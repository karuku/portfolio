document.documentElement.classList.add("js");

const filterButtons = [...document.querySelectorAll("[data-filter]")];
const projects = [...document.querySelectorAll(".project-row")];
const filterGroup = document.querySelector(".filter-group");

if (filterGroup && filterButtons.length && projects.length) {
  filterGroup.hidden = false;

  filterGroup.addEventListener("click", (event) => {
    const button = event.target.closest("[data-filter]");
    if (!button) return;

    const filter = button.dataset.filter;
    for (const filterButton of filterButtons) {
      filterButton.setAttribute("aria-pressed", String(filterButton === button));
    }
    for (const project of projects) {
      project.hidden = filter !== "all" && project.dataset.category !== filter;
    }
  });
}

const year = document.querySelector("#current-year");
if (year) year.textContent = String(new Date().getFullYear());

const revealItems = document.querySelectorAll(".reveal");
const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

if ("IntersectionObserver" in window && !reducedMotion) {
  const revealObserver = new IntersectionObserver(
    (entries, observer) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue;
        entry.target.classList.add("is-visible");
        observer.unobserve(entry.target);
      }
    },
    { rootMargin: "0px 0px -48px 0px", threshold: 0.08 },
  );

  for (const item of revealItems) revealObserver.observe(item);
} else {
  for (const item of revealItems) item.classList.add("is-visible");
}