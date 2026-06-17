import { FormEvent, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createWorkstation,
  deleteWorkstation,
  getWorkstations,
  updateWorkstation,
  Workstation,
  WorkstationInput
} from '../../shared/api/workstationsApi';

const emptyForm: WorkstationInput = { shortName: '', name: '', pcName: '' };

export function WorkstationPage() {
  const queryClient = useQueryClient();
  const [form, setForm] = useState<WorkstationInput>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);

  const workstationsQuery = useQuery({ queryKey: ['workstations'], queryFn: getWorkstations });

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['workstations'] });
  const resetForm = () => {
    setForm(emptyForm);
    setEditingId(null);
  };

  const saveMutation = useMutation({
    mutationFn: () =>
      editingId ? updateWorkstation(editingId, trimForm(form)) : createWorkstation(trimForm(form)),
    onSuccess: () => {
      resetForm();
      refresh();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteWorkstation(id),
    onSuccess: () => {
      if (editingId) {
        resetForm();
      }
      refresh();
    }
  });

  function startEdit(workstation: Workstation) {
    setEditingId(workstation.id);
    setForm({
      shortName: workstation.shortName,
      name: workstation.name,
      pcName: workstation.pcName
    });
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    saveMutation.mutate();
  }

  const isValid = form.shortName.trim() && form.name.trim() && form.pcName.trim();
  const actionError = saveMutation.error ?? deleteMutation.error;

  return (
    <section className="panel">
      <div className="panel-head">
        <h1>Workstations</h1>
      </div>

      <form className="form grid-form" onSubmit={submit}>
        <input
          value={form.shortName}
          onChange={(event) => setForm({ ...form, shortName: event.target.value })}
          placeholder="Short name"
          aria-label="Short name"
        />
        <input
          value={form.name}
          onChange={(event) => setForm({ ...form, name: event.target.value })}
          placeholder="Name"
          aria-label="Name"
        />
        <input
          value={form.pcName}
          onChange={(event) => setForm({ ...form, pcName: event.target.value })}
          placeholder="PC name"
          aria-label="PC name"
        />
        <div className="actions">
          <button type="submit" disabled={saveMutation.isPending || !isValid}>
            {editingId ? 'Save changes' : 'Add workstation'}
          </button>
          {editingId && (
            <button type="button" className="ghost" onClick={resetForm}>
              Cancel
            </button>
          )}
        </div>
      </form>

      {workstationsQuery.isLoading && <p>Loading workstations…</p>}
      {workstationsQuery.isError && <p className="error">{(workstationsQuery.error as Error).message}</p>}
      {actionError && <p className="error">{(actionError as Error).message}</p>}

      <table className="data-table">
        <thead>
          <tr>
            <th>Short name</th>
            <th>Name</th>
            <th>PC name</th>
            <th aria-label="Actions" />
          </tr>
        </thead>
        <tbody>
          {workstationsQuery.data?.map((workstation) => (
            <tr key={workstation.id} className={editingId === workstation.id ? 'row-editing' : undefined}>
              <td>{workstation.shortName}</td>
              <td>{workstation.name}</td>
              <td>{workstation.pcName}</td>
              <td className="actions">
                <button type="button" className="ghost" onClick={() => startEdit(workstation)}>
                  Edit
                </button>
                <button type="button" className="danger" onClick={() => deleteMutation.mutate(workstation.id)}>
                  Delete
                </button>
              </td>
            </tr>
          ))}
          {workstationsQuery.data?.length === 0 && (
            <tr>
              <td colSpan={4} className="empty">
                No workstations yet.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  );
}

function trimForm(input: WorkstationInput): WorkstationInput {
  return {
    shortName: input.shortName.trim(),
    name: input.name.trim(),
    pcName: input.pcName.trim()
  };
}
