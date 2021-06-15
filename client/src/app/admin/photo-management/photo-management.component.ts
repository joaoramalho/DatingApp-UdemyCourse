import { Component, OnInit } from '@angular/core';
import { take } from 'rxjs/operators';
import { Photo } from 'src/app/_models/photo';
import { AdminService } from 'src/app/_services/admin.service';

@Component({
  selector: 'app-photo-management',
  templateUrl: './photo-management.component.html',
  styleUrls: ['./photo-management.component.css']
})
export class PhotoManagementComponent implements OnInit {
  userPhotos: Photo[];

  constructor(private adminService: AdminService) { }

  ngOnInit(): void {
    this.getPhotosForApproval();
  }

  getPhotosForApproval(){
    this.adminService.getPhotosForApproval().subscribe(resp => {
      this.userPhotos = resp;
    });
  }

  approvePhoto(photoId: number){
    this.adminService.approvePhoto(photoId).subscribe();
  }

  rejectPhoto(photoId: number){
    this.adminService.rejectPhoto(photoId).subscribe();
  }

}
