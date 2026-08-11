import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
@Injectable({ providedIn: "root" })

/**
 * Get Data from the server for the home page
 */
export class HomeService {
private readonly backendUrl: string = "http://localhost:5079/api";
private readonly baseUrl: string = `${this.backendUrl}/home`;


constructor(private readonly http: HttpClient) {}

    /** GET /api/home/checkBackendConnection */
    public getAPIConnection(): Observable<any> {
        return this.http.get(`${this.baseUrl}/checkbackendconnection`);
    }
}