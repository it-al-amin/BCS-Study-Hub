import { Fragment, type ReactNode } from 'react'

/**
 * A deliberately small, safe Markdown subset rendered straight to React elements. No raw HTML is ever injected,
 * so stored content cannot run scripts. Supported: # headings, paragraphs, - / * bullet lists, 1. numbered lists,
 * > quotes, **bold**, *italic*, `code`, and [links](https://…) limited to http(s) and mailto.
 */
export function Markdown({ source, className = '' }: { source: string | null | undefined; className?: string }) {
  if (!source?.trim()) return null
  return <div className={`space-y-3 text-sm leading-7 ${className}`}>{renderBlocks(source)}</div>
}

function renderBlocks(source: string): ReactNode[] {
  const lines = source.replace(/\r\n?/g, '\n').split('\n')
  const blocks: ReactNode[] = []
  let index = 0
  while (index < lines.length) {
    const line = lines[index]
    if (!line.trim()) {
      index++
      continue
    }

    const heading = /^(#{1,3})\s+(.*)$/.exec(line)
    if (heading) {
      const level = heading[1].length
      const content = inline(heading[2], `h${index}`)
      blocks.push(level === 1
        ? <h2 key={index} className="text-lg font-semibold">{content}</h2>
        : level === 2 ? <h3 key={index} className="text-base font-semibold">{content}</h3>
          : <h4 key={index} className="font-semibold">{content}</h4>)
      index++
      continue
    }

    if (/^\s*[-*]\s+/.test(line) || /^\s*\d+[.)]\s+/.test(line)) {
      const ordered = /^\s*\d+[.)]\s+/.test(line)
      const pattern = ordered ? /^\s*\d+[.)]\s+(.*)$/ : /^\s*[-*]\s+(.*)$/
      const items: ReactNode[] = []
      while (index < lines.length && pattern.test(lines[index])) {
        items.push(<li key={index}>{inline(pattern.exec(lines[index])![1], `li${index}`)}</li>)
        index++
      }
      blocks.push(ordered
        ? <ol key={`ol${index}`} className="list-decimal space-y-1 pl-6">{items}</ol>
        : <ul key={`ul${index}`} className="list-disc space-y-1 pl-6">{items}</ul>)
      continue
    }

    if (/^\s*>\s?/.test(line)) {
      const quoted: string[] = []
      while (index < lines.length && /^\s*>\s?/.test(lines[index])) {
        quoted.push(lines[index].replace(/^\s*>\s?/, ''))
        index++
      }
      blocks.push(<blockquote key={`q${index}`} className="border-l-4 border-slate-300 pl-3 text-slate-600 dark:border-slate-600 dark:text-slate-300">{withBreaks(quoted, `q${index}`)}</blockquote>)
      continue
    }

    const paragraph: string[] = []
    while (index < lines.length && lines[index].trim() && !/^(#{1,3})\s+/.test(lines[index]) && !/^\s*([-*]|\d+[.)])\s+/.test(lines[index]) && !/^\s*>/.test(lines[index])) {
      paragraph.push(lines[index])
      index++
    }
    blocks.push(<p key={`p${index}`}>{withBreaks(paragraph, `p${index}`)}</p>)
  }
  return blocks
}

function withBreaks(lines: string[], key: string) {
  return lines.map((text, position) => <Fragment key={`${key}-${position}`}>{position > 0 && <br />}{inline(text, `${key}-${position}`)}</Fragment>)
}

const tokenPattern = /(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)\s]+\)|\*[^*\s][^*]*\*|_[^_\s][^_]*_)/g

function inline(text: string, key: string): ReactNode[] {
  return text.split(tokenPattern).filter((part) => part !== '').map((part, position) => {
    const id = `${key}-${position}`
    if (part.startsWith('**') && part.endsWith('**') && part.length > 4) return <strong key={id}>{part.slice(2, -2)}</strong>
    if (part.startsWith('`') && part.endsWith('`') && part.length > 2) return <code key={id} className="rounded bg-slate-100 px-1 text-[0.9em] dark:bg-slate-800">{part.slice(1, -1)}</code>
    const link = /^\[([^\]]+)\]\(([^)\s]+)\)$/.exec(part)
    if (link) {
      const href = safeHref(link[2])
      return href
        ? <a key={id} href={href} target={href.startsWith('mailto:') ? undefined : '_blank'} rel="noopener noreferrer nofollow" className="text-emerald-700 underline dark:text-emerald-400">{link[1]}</a>
        : <span key={id}>{link[1]}</span>
    }
    if ((part.startsWith('*') && part.endsWith('*')) || (part.startsWith('_') && part.endsWith('_'))) {
      if (part.length > 2) return <em key={id}>{part.slice(1, -1)}</em>
    }
    return <Fragment key={id}>{part}</Fragment>
  })
}

function safeHref(value: string) {
  try {
    const url = new URL(value)
    return url.protocol === 'https:' || url.protocol === 'http:' || url.protocol === 'mailto:' ? url.toString() : null
  } catch {
    return null
  }
}
