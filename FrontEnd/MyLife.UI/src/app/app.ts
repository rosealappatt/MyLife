import { Component, inject, OnInit, signal } from '@angular/core';
//import { RouterOutlet } from '@angular/router';
import { HomeService } from './Services/home.service';

@Component({
  selector: 'app-root',
  //imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  // Service Injection
  private readonly homeService = inject(HomeService);

  // Signals
  protected readonly title = signal(null);
  protected readonly apiConnectionStatus = signal<string | null>(null);


  constructor() {}

  ngOnInit() {
    this.checkAPIConnection();
  }

  checkAPIConnection() {
    this.homeService.getAPIConnection().subscribe({
      next: (data) => {
        this.title.set(data.title);
        this.apiConnectionStatus.set(data.message);
        console.log('API Connection Successful:', data.message);
      },
      error: (error) => {
        this.title.set(null);
        this.apiConnectionStatus.set('Disconnected');
        console.error('API Connection Failed:', error);
      }
    });
  }
}
