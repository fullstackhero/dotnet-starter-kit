// Posts a view beacon for the current /docs/* page and renders the
// returned count into [data-views-count]. The chip uses a data-state
// reveal so the count-up animation runs once when the network resolves.
// Idempotent against Astro's view-transitions lifecycle: also re-runs
// on `astro:page-load`.

interface ViewsResponse {
  views: number;
}

const RUN_FLAG = '__fshViewsBeaconBound';
const COUNT_DURATION_MS = 900;

const reduceMotion =
  typeof window !== 'undefined' &&
  window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;

function formatCount(n: number): string {
  try {
    return new Intl.NumberFormat(undefined).format(n);
  } catch {
    return String(n);
  }
}

// easeOutCubic — fast at first, soft landing. Feels like the number is
// "settling in" rather than ticking up linearly.
function easeOutCubic(t: number): number {
  return 1 - Math.pow(1 - t, 3);
}

function animateCount(el: HTMLElement, to: number): void {
  if (reduceMotion || to <= 0) {
    el.textContent = formatCount(to);
    return;
  }
  const start = performance.now();
  const step = (now: number): void => {
    const t = Math.min(1, (now - start) / COUNT_DURATION_MS);
    const value = Math.round(to * easeOutCubic(t));
    el.textContent = formatCount(value);
    if (t < 1) requestAnimationFrame(step);
  };
  requestAnimationFrame(step);
}

async function sendBeacon(): Promise<void> {
  const slug = location.pathname.replace(/\/+$/, '') || '/';
  if (!slug.startsWith('/docs')) return;

  const wrap = document.querySelector<HTMLElement>('[data-views-count-wrap]');
  const target = document.querySelector<HTMLElement>('[data-views-count]');
  if (!wrap || !target) return;

  try {
    const res = await fetch('/api/views', {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ slug }),
      keepalive: true,
    });
    if (!res.ok) return;
    const data = (await res.json()) as ViewsResponse;
    if (typeof data.views !== 'number') return;

    // Reveal first so the CSS transition can play; then animate the digits
    // in parallel. Two-step is intentional: the chip glides in while the
    // number counts up — composed, not stacked.
    wrap.dataset.state = 'ready';
    animateCount(target, data.views);
  } catch {
    // Network failure is silent — the chip stays in `pending` state and
    // never appears. (No flash, no broken layout.)
  }
}

function init(): void {
  // Defer past initial paint so the beacon never competes with content.
  if ('requestIdleCallback' in window) {
    (window as Window & {
      requestIdleCallback: (cb: () => void, opts?: { timeout: number }) => void;
    }).requestIdleCallback(() => sendBeacon(), { timeout: 2000 });
  } else {
    setTimeout(sendBeacon, 0);
  }
}

const w = window as Window & { [RUN_FLAG]?: boolean };
if (!w[RUN_FLAG]) {
  w[RUN_FLAG] = true;
  init();
  document.addEventListener('astro:page-load', init);
}
