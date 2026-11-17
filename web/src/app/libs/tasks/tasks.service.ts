import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateTaskRequest, SiteTask, TaskQuery, UpdateTaskRequest } from './task.model';

/** Tasks of the sites and of the company. Owner and Manager read and write; only Owner deletes (the API checks it). */
@Injectable({ providedIn: 'root' })
export class TasksService {
    private readonly http = inject(HttpClient);

    list(query: TaskQuery = {}): Observable<SiteTask[]> {
        let params = new HttpParams();
        if (query.siteId) {
            params = params.set('siteId', query.siteId);
        }
        if (query.assigneeId) {
            params = params.set('assigneeId', query.assigneeId);
        }
        if (query.scope) {
            params = params.set('scope', query.scope);
        }
        return this.http.get<SiteTask[]>('/api/tasks', { params });
    }

    create(request: CreateTaskRequest): Observable<SiteTask> {
        return this.http.post<SiteTask>('/api/tasks', request);
    }

    update(id: string, request: UpdateTaskRequest): Observable<SiteTask> {
        return this.http.put<SiteTask>(`/api/tasks/${id}`, request);
    }

    setDone(id: string, done: boolean): Observable<SiteTask> {
        return this.http.put<SiteTask>(`/api/tasks/${id}/done`, { done });
    }

    remove(id: string): Observable<void> {
        return this.http.delete<void>(`/api/tasks/${id}`);
    }
}
