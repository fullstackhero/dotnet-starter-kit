/**
 * Magnetic tilt effects (ported from codewithmukesh/blog).
 *
 * Two variants, both pointer-tracked perspective transforms:
 *
 *   [data-magnetic-card] — small tilt + 4px lift. Used on cards.
 *   [data-magnetic-ide]  — slightly larger tilt + scale + radial spotlight
 *                          via --mouse-x / --mouse-y custom properties.
 *                          Used on hero-class editor mockups.
 *
 * Re-binds idempotently — safe to call after astro:after-swap.
 * No-op on touch devices and when prefers-reduced-motion is set.
 */
export function initMagneticEffects() {
  if (typeof window === 'undefined') return;
  if (window.matchMedia('(hover: none)').matches) return;
  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

  // ── data-magnetic-card ──────────────────────────────────────────────
  document.querySelectorAll<HTMLElement>('[data-magnetic-card]').forEach((card) => {
    if (card.dataset.magneticInit) return;
    card.dataset.magneticInit = 'true';

    let rafId: number | null = null;

    card.addEventListener('mouseenter', () => {
      card.style.willChange = 'transform';
    });

    card.addEventListener('mousemove', (e: MouseEvent) => {
      if (rafId) cancelAnimationFrame(rafId);
      rafId = requestAnimationFrame(() => {
        const rect = card.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        const cx = rect.width / 2;
        const cy = rect.height / 2;
        const rotateX = ((y - cy) / cy) * -3;
        const rotateY = ((x - cx) / cx) * 3;
        card.style.transition = 'transform 0.08s linear';
        card.style.transform = `perspective(800px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) translateY(-4px)`;
      });
    });

    card.addEventListener('mouseleave', () => {
      if (rafId) cancelAnimationFrame(rafId);
      card.style.transition = 'transform 0.5s cubic-bezier(0.23, 1, 0.32, 1)';
      card.style.transform = '';
      card.style.willChange = '';
    });
  });

  // ── data-magnetic-ide ───────────────────────────────────────────────
  document.querySelectorAll<HTMLElement>('[data-magnetic-ide]').forEach((block) => {
    if (block.dataset.magneticInit) return;
    block.dataset.magneticInit = 'true';

    const maxTilt = 5;
    const scale = 1.015;
    let rafId: number | null = null;

    block.addEventListener('mouseenter', () => {
      block.style.willChange = 'transform';
      block.style.transition = 'transform 0.15s ease-out, box-shadow 0.2s ease-out';
    });

    block.addEventListener('mousemove', (e: MouseEvent) => {
      if (rafId) cancelAnimationFrame(rafId);
      rafId = requestAnimationFrame(() => {
        const rect = block.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        const cx = rect.width / 2;
        const cy = rect.height / 2;
        const rotateX = ((y - cy) / cy) * -maxTilt;
        const rotateY = ((x - cx) / cx) * maxTilt;
        block.style.transform = `perspective(900px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale3d(${scale}, ${scale}, ${scale})`;
        block.style.setProperty('--mouse-x', `${x}px`);
        block.style.setProperty('--mouse-y', `${y}px`);
      });
    });

    block.addEventListener('mouseleave', () => {
      if (rafId) cancelAnimationFrame(rafId);
      block.style.transition = 'transform 0.5s cubic-bezier(0.22, 1, 0.36, 1), box-shadow 0.5s ease';
      block.style.transform = '';
      block.style.willChange = '';
    });
  });
}

// Backwards-compatible alias (some imports may use the old name).
export const initMagneticCards = initMagneticEffects;
