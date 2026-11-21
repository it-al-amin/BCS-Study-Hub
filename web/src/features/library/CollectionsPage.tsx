import { FolderPlus, Save, Trash2, X } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { libraryApi, type LibraryCollection, type LibraryCollectionDetail } from '../../services/library'
import { SignInRequired } from './LibraryPage'
import { errorMessage, kindLabels } from './libraryFormat'
import { KindIcon, VisibilityBadge } from './libraryUi'

export function CollectionsPage() {
  const { accessToken } = useAuth()
  const [collections, setCollections] = useState<LibraryCollection[] | null>(null)
  const [selected, setSelected] = useState<LibraryCollectionDetail | null>(null)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [error, setError] = useState('')

  const loadList = useCallback(async () => {
    if (!accessToken) return
    try {
      setCollections(await libraryApi.getCollections(accessToken))
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to load collections.'))
    }
  }, [accessToken])

  useEffect(() => {
    if (!accessToken) return
    let active = true
    libraryApi.getCollections(accessToken)
      .then((result) => active && setCollections(result))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load collections.')))
    return () => { active = false }
  }, [accessToken])

  async function open(id: string) {
    if (!accessToken) return
    setError('')
    try {
      const detail = await libraryApi.getCollection(id, accessToken)
      setSelected(detail)
      setEditingId(null)
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to open the collection.'))
    }
  }

  async function save() {
    if (!accessToken) return
    setError('')
    try {
      const input = { name: name.trim(), description: description.trim() || null }
      const saved = editingId
        ? await libraryApi.updateCollection(editingId, input, accessToken)
        : await libraryApi.createCollection(input, accessToken)
      setName('')
      setDescription('')
      setEditingId(null)
      await loadList()
      await open(saved.id)
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to save the collection.'))
    }
  }

  async function remove(collection: LibraryCollection) {
    if (!accessToken || !window.confirm(`Delete the collection "${collection.name}"? Resources themselves are not deleted.`)) return
    await libraryApi.deleteCollection(collection.id, accessToken)
    if (selected?.collection.id === collection.id) setSelected(null)
    await loadList()
  }

  async function removeItem(resourceId: string) {
    if (!accessToken || !selected) return
    await libraryApi.removeCollectionItem(selected.collection.id, resourceId, accessToken)
    await open(selected.collection.id)
    await loadList()
  }

  if (!accessToken) return <><PageHeader title="Collections" /><SignInRequired /></>

  return (
    <>
      <PageHeader title="Collections" description="Group books, files, links and notes into personal reading lists, for example by paper, cadre or revision cycle. Collections are private." />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      <div className="grid gap-5 xl:grid-cols-[minmax(16rem,0.7fr)_minmax(0,1.3fr)]">
        <div className="space-y-5">
          <Card className="p-5">
            <h2 className="font-semibold">{editingId ? 'Rename collection' : 'New collection'}</h2>
            <form className="mt-3 space-y-2" onSubmit={(event) => { event.preventDefault(); void save() }}>
              <input aria-label="Collection name" required maxLength={150} placeholder="Name" value={name} onChange={(event) => setName(event.target.value)} className="h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950" />
              <textarea aria-label="Collection description" maxLength={1000} rows={2} placeholder="Description (optional)" value={description} onChange={(event) => setDescription(event.target.value)} className="w-full rounded-lg border border-slate-200 bg-white p-3 text-sm dark:border-slate-700 dark:bg-slate-950" />
              <div className="flex gap-2"><Button type="submit" size="sm" disabled={!name.trim()}>{editingId ? <><Save className="h-4 w-4" />Save</> : <><FolderPlus className="h-4 w-4" />Create</>}</Button>{editingId && <Button size="sm" variant="ghost" onClick={() => { setEditingId(null); setName(''); setDescription('') }}>Cancel</Button>}</div>
            </form>
          </Card>
          <Card className="overflow-hidden">
            <CardHeader title="Your collections" />
            {!collections ? <LoadingState /> : collections.length === 0 ? <p className="p-5 text-sm text-slate-500">No collections yet.</p>
              : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{collections.map((collection) => <li key={collection.id} className={`flex items-center gap-2 px-4 py-3 ${selected?.collection.id === collection.id ? 'bg-emerald-50 dark:bg-emerald-500/10' : ''}`}><button type="button" className="min-w-0 flex-1 text-left" onClick={() => void open(collection.id)}><span className="block truncate text-sm font-medium">{collection.name}</span><span className="block text-xs text-slate-500">{collection.itemCount} resources</span></button><button type="button" className="text-xs text-slate-500 hover:text-slate-800 dark:hover:text-slate-200" onClick={() => { setEditingId(collection.id); setName(collection.name); setDescription(collection.description ?? '') }}>Rename</button><button type="button" aria-label={`Delete ${collection.name}`} className="text-slate-400 hover:text-red-600" onClick={() => void remove(collection)}><Trash2 className="h-4 w-4" /></button></li>)}</ul>}
          </Card>
        </div>
        <Card className="overflow-hidden">
          {!selected ? <EmptyState title="Select a collection" description="Add resources to a collection from any resource or book page." />
            : <>
              <CardHeader title={selected.collection.name} subtitle={selected.collection.description ?? `${selected.items.length} resources`} />
              {selected.items.length === 0 ? <EmptyState title="This collection is empty" description="Open a resource and choose “Add to collection”." />
                : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{selected.items.map((resource) => <li key={resource.id} className="flex items-center gap-3 px-5 py-3"><KindIcon resource={resource} /><Link to={`/library/resources/${resource.id}`} className="min-w-0 flex-1"><span className="block truncate text-sm font-medium hover:underline">{resource.title}</span><span className="block truncate text-xs text-slate-500">{[kindLabels[resource.kind], resource.authors, resource.progress && `${resource.progress.percentComplete}% read`].filter(Boolean).join(' · ')}</span></Link><VisibilityBadge resource={resource} /><button type="button" aria-label={`Remove ${resource.title}`} className="text-slate-400 hover:text-red-600" onClick={() => void removeItem(resource.id)}><X className="h-4 w-4" /></button></li>)}</ul>}
            </>}
        </Card>
      </div>
    </>
  )
}
