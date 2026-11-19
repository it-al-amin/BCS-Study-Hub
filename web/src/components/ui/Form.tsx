import type { InputHTMLAttributes, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react'

const fieldClass = 'mt-1.5 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/10 dark:border-slate-700 dark:bg-slate-800'

export function Input(props: InputHTMLAttributes<HTMLInputElement>) { return <input {...props} className={`${fieldClass} ${props.className ?? ''}`} /> }
export function Select(props: SelectHTMLAttributes<HTMLSelectElement>) { return <select {...props} className={`${fieldClass} ${props.className ?? ''}`} /> }
export function Textarea(props: TextareaHTMLAttributes<HTMLTextAreaElement>) { return <textarea {...props} className={`mt-1.5 min-h-28 w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/10 dark:border-slate-700 dark:bg-slate-800 ${props.className ?? ''}`} /> }
export function Field({ label, children, htmlFor }: { label: string; children: React.ReactNode; htmlFor?: string }) { return <label htmlFor={htmlFor} className="block text-sm font-medium text-slate-700 dark:text-slate-300">{label}{children}</label> }
