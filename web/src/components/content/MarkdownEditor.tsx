import { Bold, Heading2, Italic, Link2, List, ListOrdered, Quote } from 'lucide-react'
import { useRef, useState } from 'react'
import { Markdown } from './Markdown'

/**
 * Long-form editor for notes and content: a formatting toolbar that inserts Markdown, a write/preview toggle that
 * shows exactly what learners will see (rendered by the same safe renderer), and a live character count.
 */
type ToolAction = 'bold' | 'italic' | 'heading' | 'bullets' | 'numbers' | 'quote' | 'link'

const tools: { label: string; action: ToolAction; icon: typeof Bold }[] = [
  { label: 'Bold', action: 'bold', icon: Bold },
  { label: 'Italic', action: 'italic', icon: Italic },
  { label: 'Heading', action: 'heading', icon: Heading2 },
  { label: 'Bullet list', action: 'bullets', icon: List },
  { label: 'Numbered list', action: 'numbers', icon: ListOrdered },
  { label: 'Quote', action: 'quote', icon: Quote },
  { label: 'Link', action: 'link', icon: Link2 },
]

export function MarkdownEditor({
  value,
  onChange,
  label,
  maxLength,
  rows = 12,
  placeholder,
  required,
}: {
  value: string
  onChange: (value: string) => void
  label: string
  maxLength: number
  rows?: number
  placeholder?: string
  required?: boolean
}) {
  const [mode, setMode] = useState<'write' | 'preview'>('write')
  const area = useRef<HTMLTextAreaElement>(null)

  function wrap(before: string, after = before, placeholderText = 'text') {
    const element = area.current
    if (!element) return
    const start = element.selectionStart
    const end = element.selectionEnd
    const selected = value.slice(start, end) || placeholderText
    const next = `${value.slice(0, start)}${before}${selected}${after}${value.slice(end)}`
    if (next.length > maxLength) return
    onChange(next)
    requestAnimationFrame(() => {
      element.focus()
      element.setSelectionRange(start + before.length, start + before.length + selected.length)
    })
  }

  function prefixLines(prefix: (index: number) => string) {
    const element = area.current
    if (!element) return
    const start = value.lastIndexOf('\n', element.selectionStart - 1) + 1
    const end = element.selectionEnd
    const block = value.slice(start, end) || 'item'
    const replaced = block.split('\n').map((line, index) => `${prefix(index)}${line}`).join('\n')
    const next = `${value.slice(0, start)}${replaced}${value.slice(end)}`
    if (next.length <= maxLength) onChange(next)
    requestAnimationFrame(() => element.focus())
  }

  function apply(action: ToolAction) {
    if (action === 'bold') wrap('**')
    else if (action === 'italic') wrap('*')
    else if (action === 'heading') prefixLines(() => '## ')
    else if (action === 'bullets') prefixLines(() => '- ')
    else if (action === 'numbers') prefixLines((index) => `${index + 1}. `)
    else if (action === 'quote') prefixLines(() => '> ')
    else wrap('[', '](https://)', 'link text')
  }

  return (
    <div className="block text-xs font-medium text-slate-600 dark:text-slate-300">
      <div className="flex items-center justify-between gap-2">
        <span>{label}</span>
        <div className="flex rounded-md border border-slate-200 text-[11px] dark:border-slate-700" role="tablist" aria-label={`${label} mode`}>
          {(['write', 'preview'] as const).map((value_) => <button key={value_} type="button" role="tab" aria-selected={mode === value_} onClick={() => setMode(value_)} className={`px-2 py-0.5 capitalize ${mode === value_ ? 'bg-slate-100 dark:bg-slate-800' : ''}`}>{value_}</button>)}
        </div>
      </div>
      <div className="mt-1 overflow-hidden rounded-lg border border-slate-200 dark:border-slate-700">
        {mode === 'write' ? <>
          <div className="flex flex-wrap gap-0.5 border-b border-slate-200 bg-slate-50 p-1 dark:border-slate-700 dark:bg-slate-900">
            {tools.map((tool) => <button key={tool.label} type="button" aria-label={tool.label} title={tool.label} onClick={() => apply(tool.action)} className="rounded p-1.5 text-slate-600 hover:bg-white dark:text-slate-300 dark:hover:bg-slate-800"><tool.icon className="h-4 w-4" /></button>)}
          </div>
          <textarea ref={area} aria-label={label} required={required} maxLength={maxLength} rows={rows} placeholder={placeholder} value={value} onChange={(event) => onChange(event.target.value)} className="block w-full resize-y bg-white p-3 font-mono text-sm font-normal leading-6 text-slate-900 outline-none dark:bg-slate-950 dark:text-slate-100" />
        </> : <div className="min-h-40 bg-white p-4 font-normal text-slate-800 dark:bg-slate-950 dark:text-slate-100">{value.trim() ? <Markdown source={value} /> : <p className="text-sm text-slate-400">Nothing to preview.</p>}</div>}
      </div>
      <p className="mt-1 text-right font-normal text-slate-500">{value.length.toLocaleString()} / {maxLength.toLocaleString()} · Markdown: **bold**, *italic*, ## heading, - list, [link](https://…)</p>
    </div>
  )
}
