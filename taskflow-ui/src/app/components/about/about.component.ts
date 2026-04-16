import { Component } from '@angular/core';

@Component({
  selector: 'app-about',
  templateUrl: './about.component.html',
  styleUrls: ['./about.component.css']
})
export class AboutComponent {
  readonly liveUrl  = 'https://plasmacat420.github.io/TaskFlowAPI/';
  readonly apiUrl   = 'https://taskflowapi-gydh.onrender.com/api';
  readonly githubUrl = 'https://github.com/plasmacat420/TaskFlowAPI';
}
