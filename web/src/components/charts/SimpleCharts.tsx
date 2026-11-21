/**
 * Minimal single-series charts. One hue (the app's emerald), recessive axes, rounded data-ends anchored to the
 * baseline, 2px gaps, per-bar hover titles, and an accessible table alternative. Single series need no legend.
 */

export function ColumnChart({ data, label, format = String, height = 140 }: {
  data: { key: string; label: string; value: number }[]
  label: string
  format?: (value: number) => string
  height?: number
}) {
  const max = Math.max(1, ...data.map((item) => item.value))
  return (
    <figure>
      <div role="img" aria-label={`${label}: ${data.map((item) => `${item.label} ${format(item.value)}`).join(', ')}`} className="flex items-end gap-[2px] border-b border-slate-200 dark:border-slate-700" style={{ height }}>
        {data.map((item) => (
          <div key={item.key} className="group relative flex h-full min-w-0 flex-1 items-end" title={`${item.label}: ${format(item.value)}`}>
            <div className="w-full rounded-t bg-emerald-600 transition-colors group-hover:bg-emerald-700 dark:bg-emerald-500 dark:group-hover:bg-emerald-400" style={{ height: `${(item.value / max) * 100}%`, minHeight: item.value > 0 ? 2 : 0 }} />
            <span className="pointer-events-none absolute -top-6 left-1/2 hidden -translate-x-1/2 whitespace-nowrap rounded bg-slate-900 px-1.5 py-0.5 text-[11px] text-white group-hover:block dark:bg-slate-100 dark:text-slate-900">{format(item.value)}</span>
          </div>
        ))}
      </div>
      <div className="mt-1 flex gap-[2px] text-[10px] text-slate-500">{data.map((item, index) => <span key={item.key} className="min-w-0 flex-1 truncate text-center">{data.length <= 12 || index % Math.ceil(data.length / 10) === 0 ? item.label : ''}</span>)}</div>
      <details className="mt-2 text-xs text-slate-500"><summary className="cursor-pointer">View as table</summary>
        <table className="mt-1 w-full text-left"><tbody>{data.map((item) => <tr key={item.key}><td className="py-0.5 pr-3">{item.label}</td><td className="py-0.5 tabular-nums">{format(item.value)}</td></tr>)}</tbody></table>
      </details>
    </figure>
  )
}

export function BarList({ data, format = String, max }: {
  data: { key: string; label: string; value: number; detail?: string }[]
  format?: (value: number) => string
  max?: number
}) {
  const scale = max ?? Math.max(1, ...data.map((item) => item.value))
  return (
    <ul className="space-y-2">
      {data.map((item) => (
        <li key={item.key} title={`${item.label}: ${format(item.value)}${item.detail ? ` (${item.detail})` : ''}`}>
          <div className="flex justify-between gap-3 text-sm"><span className="truncate">{item.label}</span><span className="shrink-0 tabular-nums text-slate-600 dark:text-slate-300">{format(item.value)}{item.detail && <span className="ml-1 text-xs text-slate-500">{item.detail}</span>}</span></div>
          <div className="mt-1 h-2 rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-2 rounded-full bg-emerald-600 dark:bg-emerald-500" style={{ width: `${Math.min(100, (item.value / scale) * 100)}%` }} /></div>
        </li>
      ))}
    </ul>
  )
}

export function StatTile({ label, value, detail }: { label: string; value: string | number; detail?: string }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
      <p className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</p>
      <p className="mt-1.5 text-2xl font-semibold tabular-nums">{value}</p>
      {detail && <p className="mt-0.5 text-xs text-slate-500">{detail}</p>}
    </div>
  )
}
